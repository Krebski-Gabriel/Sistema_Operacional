namespace SimuladorSO.Nucleo.Memoria;

/// <summary>Entrada da tabela de páginas: indica presença e moldura física.</summary>
public sealed class EntradaTabelaPaginas
{
    public bool Presente { get; set; }
    public int Moldura { get; set; } = -1;
    public long Pagina { get; }
    public EntradaTabelaPaginas(long pagina) => Pagina = pagina;
}

/// <summary>
/// Tabela de páginas de um processo. Mapeia número de página lógica para a
/// entrada correspondente. As páginas são criadas sob demanda.
/// </summary>
public sealed class TabelaDePaginas
{
    private readonly Dictionary<long, EntradaTabelaPaginas> _entradas = new();

    public EntradaTabelaPaginas Obter(long pagina)
    {
        if (!_entradas.TryGetValue(pagina, out var e))
        {
            e = new EntradaTabelaPaginas(pagina);
            _entradas[pagina] = e;
        }
        return e;
    }

    public bool TemEntrada(long pagina) => _entradas.ContainsKey(pagina);

    public IEnumerable<EntradaTabelaPaginas> Entradas => _entradas.Values;
}

/// <summary>Moldura física (frame). Tamanho fixo, igual para todas.</summary>
public sealed class Moldura
{
    public int Indice { get; }
    public bool Ocupada { get; set; }
    public int ProcessoId { get; set; } = -1;
    public long Pagina { get; set; } = -1;
    public EntradaTabelaPaginas? Entrada { get; set; }
    public Moldura(int indice) => Indice = indice;
}
