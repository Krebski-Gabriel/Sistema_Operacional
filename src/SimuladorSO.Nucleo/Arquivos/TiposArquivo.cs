namespace SimuladorSO.Nucleo.Arquivos;

public enum TipoNo { Arquivo, Diretorio }

/// <summary>Permissões básicas de um objeto do sistema de arquivos.</summary>
public sealed class Permissoes
{
    public bool Leitura { get; set; } = true;
    public bool Escrita { get; set; } = true;
    public Permissoes() { }
    public Permissoes(bool leitura, bool escrita) { Leitura = leitura; Escrita = escrita; }
    public override string ToString() => $"{(Leitura ? "r" : "-")}{(Escrita ? "w" : "-")}";
}

/// <summary>
/// FCB / nó-i simplificado. Todo objeto persistente possui nome, tipo, tamanho,
/// permissões, instantes lógicos de criação/modificação e, para arquivos, a
/// lista de blocos de dados (alocação encadeada).
/// </summary>
public sealed class No
{
    public string Nome { get; set; }
    public TipoNo Tipo { get; }
    public long Tamanho { get; set; }
    public Permissoes Permissoes { get; }
    public long CriacaoLogica { get; }
    public long ModificacaoLogica { get; set; }
    public No? Pai { get; set; }

    /// <summary>Filhos, para diretórios (nome -> nó).</summary>
    public Dictionary<string, No> Filhos { get; } = new();

    /// <summary>Blocos de dados encadeados, para arquivos.</summary>
    public List<int> Blocos { get; } = new();

    public No(string nome, TipoNo tipo, long clock, Permissoes? permissoes = null)
    {
        Nome = nome;
        Tipo = tipo;
        CriacaoLogica = clock;
        ModificacaoLogica = clock;
        Permissoes = permissoes ?? new Permissoes();
    }

    public string CaminhoAbsoluto()
    {
        if (Pai == null) return "/";
        var partes = new List<string>();
        var atual = this;
        while (atual?.Pai != null) { partes.Add(atual.Nome); atual = atual.Pai; }
        partes.Reverse();
        return "/" + string.Join("/", partes);
    }
}

/// <summary>Entrada da tabela global de arquivos abertos (compartilhada).</summary>
public sealed class EntradaTabelaGlobal
{
    public int Id { get; }
    public No No { get; }
    public int Referencias { get; set; }
    public EntradaTabelaGlobal(int id, No no) { Id = id; No = no; }
}

/// <summary>Descritor local a um processo, associado a uma entrada da tabela global.</summary>
public sealed class DescritorAberto
{
    public EntradaTabelaGlobal EntradaGlobal { get; }
    public bool ModoEscrita { get; }
    public long Posicao { get; set; }
    public DescritorAberto(EntradaTabelaGlobal entrada, bool escrita)
    {
        EntradaGlobal = entrada; ModoEscrita = escrita;
    }
}

/// <summary>Sinaliza operação inválida sobre o sistema de arquivos.</summary>
public sealed class ExcecaoSistemaArquivos : Exception
{
    public ExcecaoSistemaArquivos(string mensagem) : base(mensagem) { }
}
