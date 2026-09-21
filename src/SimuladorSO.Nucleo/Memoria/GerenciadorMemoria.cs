namespace SimuladorSO.Nucleo.Memoria;

/// <summary>Resultado da consulta de um acesso à memória.</summary>
public enum ResultadoAcesso { Acerto, Falta }

/// <summary>Descreve a substituição ocorrida durante o tratamento de uma falta.</summary>
public readonly struct ResultadoCarga
{
    public int Moldura { get; }
    public bool HouveSubstituicao { get; }
    public int ProcessoVitima { get; }
    public long PaginaVitima { get; }
    public ResultadoCarga(int moldura, bool sub, int procVit, long pagVit)
    {
        Moldura = moldura; HouveSubstituicao = sub; ProcessoVitima = procVit; PaginaVitima = pagVit;
    }
}

/// <summary>
/// Gerência de memória por paginação, com molduras de tamanho fixo e substituição
/// global controlada por uma política substituível. Faz a tradução de endereços,
/// detecta acerto/falta e trata a falta alocando moldura livre ou uma vítima.
/// </summary>
public sealed class GerenciadorMemoria
{
    private readonly long _tamanhoPagina;
    private readonly Moldura[] _molduras;
    private readonly IPoliticaSubstituicao _politica;

    public GerenciadorMemoria(long tamanhoPagina, int numeroMolduras, IPoliticaSubstituicao politica)
    {
        if (tamanhoPagina <= 0) throw new ArgumentException("Tamanho de página deve ser positivo.");
        if (numeroMolduras <= 0) throw new ArgumentException("Número de molduras deve ser positivo.");
        _tamanhoPagina = tamanhoPagina;
        _politica = politica;
        _molduras = new Moldura[numeroMolduras];
        for (int i = 0; i < numeroMolduras; i++) _molduras[i] = new Moldura(i);
    }

    public long TamanhoPagina => _tamanhoPagina;
    public int NumeroMolduras => _molduras.Length;
    public string NomePolitica => _politica.Nome;

    public long PaginaDe(long endereco) => endereco / _tamanhoPagina;
    public long DeslocamentoDe(long endereco) => endereco % _tamanhoPagina;

    /// <summary>
    /// Traduz o endereço e consulta a presença da página. Em caso de acerto,
    /// registra o acesso na política (para LRU). NÃO carrega páginas: o
    /// carregamento ocorre no fim do atendimento da falta.
    /// </summary>
    public ResultadoAcesso Acessar(TabelaDePaginas tabela, long endereco)
    {
        long pagina = PaginaDe(endereco);
        var entrada = tabela.Obter(pagina);
        if (entrada.Presente)
        {
            _politica.AoAcessar(entrada.Moldura);
            return ResultadoAcesso.Acerto;
        }
        return ResultadoAcesso.Falta;
    }

    /// <summary>
    /// Trata uma falta de página: usa uma moldura livre ou seleciona uma vítima
    /// pela política. Atualiza a tabela do processo e devolve o resultado.
    /// </summary>
    public ResultadoCarga TratarFalta(int processoId, TabelaDePaginas tabela, long endereco)
    {
        long pagina = PaginaDe(endereco);
        var entrada = tabela.Obter(pagina);

        int idxLivre = EncontrarMolduraLivre();
        bool houveSub = false;
        int procVit = -1; long pagVit = -1;

        int destino;
        if (idxLivre >= 0)
        {
            destino = idxLivre;
        }
        else
        {
            destino = _politica.EscolherVitima();
            var vitima = _molduras[destino];
            houveSub = true;
            procVit = vitima.ProcessoId;
            pagVit = vitima.Pagina;
            if (vitima.Entrada != null) { vitima.Entrada.Presente = false; vitima.Entrada.Moldura = -1; }
        }

        var moldura = _molduras[destino];
        moldura.Ocupada = true;
        moldura.ProcessoId = processoId;
        moldura.Pagina = pagina;
        moldura.Entrada = entrada;
        entrada.Presente = true;
        entrada.Moldura = destino;
        _politica.AoCarregar(destino);

        return new ResultadoCarga(destino, houveSub, procVit, pagVit);
    }

    /// <summary>Libera todas as molduras ocupadas por um processo finalizado.</summary>
    public void LiberarProcesso(int processoId)
    {
        foreach (var m in _molduras)
        {
            if (m.Ocupada && m.ProcessoId == processoId)
            {
                if (m.Entrada != null) { m.Entrada.Presente = false; m.Entrada.Moldura = -1; }
                m.Ocupada = false; m.ProcessoId = -1; m.Pagina = -1; m.Entrada = null;
                _politica.AoLiberar(m.Indice);
            }
        }
    }

    public int MoldurasOcupadas() => _molduras.Count(m => m.Ocupada);

    private int EncontrarMolduraLivre()
    {
        for (int i = 0; i < _molduras.Length; i++)
            if (!_molduras[i].Ocupada) return i;
        return -1;
    }
}
