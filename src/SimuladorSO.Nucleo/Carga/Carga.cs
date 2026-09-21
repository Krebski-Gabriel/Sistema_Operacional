using SimuladorSO.Nucleo.EntradaSaida;
using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Carga;

/// <summary>Declaração de um dispositivo pela carga de trabalho.</summary>
public sealed record DeclaracaoDispositivo(string Nome, TipoDispositivo Tipo, long TempoServico);

/// <summary>
/// Carga de trabalho: conjunto de processos (com threads e operações) e
/// declarações de dispositivos, além de uma semente opcional. É externa ao
/// código-fonte e validada na leitura.
/// </summary>
public sealed class Carga
{
    public List<Processo> Processos { get; } = new();
    public List<DeclaracaoDispositivo> Dispositivos { get; } = new();
    public int? Semente { get; set; }
}

/// <summary>Erro de formato ou validação na carga de trabalho.</summary>
public sealed class ExcecaoCarga : Exception
{
    public ExcecaoCarga(string mensagem) : base(mensagem) { }
}
