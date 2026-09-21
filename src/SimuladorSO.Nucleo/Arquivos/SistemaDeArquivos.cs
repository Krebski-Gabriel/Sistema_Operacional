namespace SimuladorSO.Nucleo.Arquivos;

/// <summary>
/// Sistema de arquivos hierárquico iniciado em um diretório raiz. Implementa as
/// operações obrigatórias (criar, abrir, ler, escrever, fechar, remover, listar),
/// mantém uma tabela global de arquivos abertos e descritores locais por processo,
/// e usa alocação encadeada de blocos.
///
/// Limitação da alocação encadeada: o acesso é essencialmente sequencial — para
/// alcançar um bloco no meio de um arquivo é preciso percorrer a cadeia desde o
/// início, o que torna o acesso aleatório ineficiente (discutido no relatório).
/// </summary>
public sealed class SistemaDeArquivos
{
    private readonly No _raiz;
    private readonly int _totalBlocos;
    private readonly long _tamanhoBloco;
    private readonly Stack<int> _blocosLivres = new();
    private readonly Dictionary<int, EntradaTabelaGlobal> _tabelaGlobal = new();
    private int _proximoIdGlobal = 1;

    public SistemaDeArquivos(int totalBlocos, long tamanhoBloco)
    {
        _totalBlocos = totalBlocos;
        _tamanhoBloco = tamanhoBloco;
        for (int i = totalBlocos - 1; i >= 0; i--) _blocosLivres.Push(i);
        _raiz = new No("", TipoNo.Diretorio, 0);
    }

    public No Raiz => _raiz;
    public int BlocosLivres => _blocosLivres.Count;
    public IReadOnlyDictionary<int, EntradaTabelaGlobal> TabelaGlobal => _tabelaGlobal;

    // ---------- Resolução de caminhos ----------

    private static string[] Partes(string caminho)
        => caminho.Split('/', StringSplitOptions.RemoveEmptyEntries);

    private No ResolverDiretorioPai(string caminho, out string nome)
    {
        var partes = Partes(caminho);
        if (partes.Length == 0)
            throw new ExcecaoSistemaArquivos($"Caminho invalido: '{caminho}'.");
        nome = partes[^1];
        var atual = _raiz;
        for (int i = 0; i < partes.Length - 1; i++)
        {
            if (!atual.Filhos.TryGetValue(partes[i], out var prox) || prox.Tipo != TipoNo.Diretorio)
                throw new ExcecaoSistemaArquivos($"Diretorio inexistente no caminho: '{caminho}'.");
            atual = prox;
        }
        return atual;
    }

    public No? Resolver(string caminho)
    {
        var partes = Partes(caminho);
        var atual = _raiz;
        foreach (var p in partes)
        {
            if (!atual.Filhos.TryGetValue(p, out var prox)) return null;
            atual = prox;
        }
        return atual;
    }

    // ---------- Operações ----------

    public No CriarArquivo(string caminho, long clock, Permissoes? permissoes = null)
        => CriarNo(caminho, TipoNo.Arquivo, clock, permissoes);

    public No CriarDiretorio(string caminho, long clock, Permissoes? permissoes = null)
        => CriarNo(caminho, TipoNo.Diretorio, clock, permissoes);

    private No CriarNo(string caminho, TipoNo tipo, long clock, Permissoes? permissoes)
    {
        var pai = ResolverDiretorioPai(caminho, out var nome);
        if (pai.Filhos.ContainsKey(nome))
            throw new ExcecaoSistemaArquivos($"Ja existe objeto em '{caminho}'.");
        var no = new No(nome, tipo, clock, permissoes) { Pai = pai };
        pai.Filhos[nome] = no;
        pai.ModificacaoLogica = clock;
        return no;
    }

    public DescritorAberto Abrir(IDictionary<string, DescritorAberto> descritores,
                                 string caminho, string handle, bool escrita, long clock)
    {
        if (descritores.ContainsKey(handle))
            throw new ExcecaoSistemaArquivos($"Handle '{handle}' ja esta em uso pelo processo.");
        var no = Resolver(caminho)
            ?? throw new ExcecaoSistemaArquivos($"Arquivo inexistente: '{caminho}'.");
        if (no.Tipo != TipoNo.Arquivo)
            throw new ExcecaoSistemaArquivos($"'{caminho}' nao e um arquivo.");
        if (escrita && !no.Permissoes.Escrita)
            throw new ExcecaoSistemaArquivos($"Sem permissao de escrita em '{caminho}'.");
        if (!escrita && !no.Permissoes.Leitura)
            throw new ExcecaoSistemaArquivos($"Sem permissao de leitura em '{caminho}'.");

        var entradaGlobal = ObterOuCriarEntradaGlobal(no);
        entradaGlobal.Referencias++;
        var descritor = new DescritorAberto(entradaGlobal, escrita);
        descritores[handle] = descritor;
        return descritor;
    }

    public long Ler(IDictionary<string, DescritorAberto> descritores, string handle, long tamanho)
    {
        var d = ObterDescritor(descritores, handle);
        long disponivel = Math.Max(0, d.EntradaGlobal.No.Tamanho - d.Posicao);
        long lido = Math.Min(tamanho, disponivel);
        d.Posicao += lido;
        return lido;
    }

    public void Escrever(IDictionary<string, DescritorAberto> descritores, string handle,
                         long tamanho, long clock)
    {
        var d = ObterDescritor(descritores, handle);
        if (!d.ModoEscrita)
            throw new ExcecaoSistemaArquivos($"Descritor '{handle}' aberto somente para leitura.");
        var no = d.EntradaGlobal.No;

        long novoTamanho = Math.Max(no.Tamanho, d.Posicao + tamanho);
        int blocosNecessarios = (int)((novoTamanho + _tamanhoBloco - 1) / _tamanhoBloco);
        int adicionais = blocosNecessarios - no.Blocos.Count;
        if (adicionais > _blocosLivres.Count)
            throw new ExcecaoSistemaArquivos(
                $"Espaco insuficiente: precisa de {adicionais} blocos, ha {_blocosLivres.Count} livres.");
        for (int i = 0; i < adicionais; i++) no.Blocos.Add(_blocosLivres.Pop());

        d.Posicao += tamanho;
        no.Tamanho = novoTamanho;
        no.ModificacaoLogica = clock;
    }

    public void Fechar(IDictionary<string, DescritorAberto> descritores, string handle)
    {
        var d = ObterDescritor(descritores, handle);
        d.EntradaGlobal.Referencias--;
        if (d.EntradaGlobal.Referencias <= 0)
            _tabelaGlobal.Remove(d.EntradaGlobal.Id);
        descritores.Remove(handle);
    }

    public void Remover(string caminho, long clock)
    {
        var no = Resolver(caminho)
            ?? throw new ExcecaoSistemaArquivos($"Caminho inexistente: '{caminho}'.");
        if (no.Pai == null)
            throw new ExcecaoSistemaArquivos("Nao e possivel remover a raiz.");
        if (no.Tipo == TipoNo.Diretorio && no.Filhos.Count > 0)
            throw new ExcecaoSistemaArquivos($"Diretorio nao vazio: '{caminho}'.");
        if (no.Tipo == TipoNo.Arquivo && EstaAberto(no))
            throw new ExcecaoSistemaArquivos($"Arquivo aberto nao pode ser removido: '{caminho}'.");

        foreach (var b in no.Blocos) _blocosLivres.Push(b);
        no.Blocos.Clear();
        no.Pai.Filhos.Remove(no.Nome);
        no.Pai.ModificacaoLogica = clock;
    }

    public IReadOnlyList<No> Listar(string caminho)
    {
        var no = Resolver(caminho)
            ?? throw new ExcecaoSistemaArquivos($"Caminho inexistente: '{caminho}'.");
        if (no.Tipo != TipoNo.Diretorio)
            throw new ExcecaoSistemaArquivos($"'{caminho}' nao e um diretorio.");
        return no.Filhos.Values.OrderBy(n => n.Nome, StringComparer.Ordinal).ToList();
    }

    /// <summary>Fecha todos os descritores de um processo (usado ao finalizar).</summary>
    public void FecharTodos(IDictionary<string, DescritorAberto> descritores)
    {
        foreach (var handle in descritores.Keys.ToArray())
            Fechar(descritores, handle);
    }

    // ---------- Auxiliares ----------

    private EntradaTabelaGlobal ObterOuCriarEntradaGlobal(No no)
    {
        foreach (var e in _tabelaGlobal.Values)
            if (ReferenceEquals(e.No, no)) return e;
        var nova = new EntradaTabelaGlobal(_proximoIdGlobal++, no);
        _tabelaGlobal[nova.Id] = nova;
        return nova;
    }

    private bool EstaAberto(No no) => _tabelaGlobal.Values.Any(e => ReferenceEquals(e.No, no));

    private static DescritorAberto ObterDescritor(IDictionary<string, DescritorAberto> descritores, string handle)
    {
        if (!descritores.TryGetValue(handle, out var d))
            throw new ExcecaoSistemaArquivos($"Descritor '{handle}' nao esta aberto.");
        return d;
    }
}
