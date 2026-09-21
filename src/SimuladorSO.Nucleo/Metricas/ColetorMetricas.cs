using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Metricas;

/// <summary>Métricas acumuladas de uma thread.</summary>
public sealed class MetricasThread
{
    public string Identificacao { get; init; } = "";
    public long Chegada { get; set; }
    public long PrimeiraCpu { get; set; } = -1;
    public long Fim { get; set; } = -1;
    public long EsperaAcumulada { get; set; }
    public long EntradaProntoEm { get; set; } = -1;

    public long TempoRetorno => Fim >= 0 ? Fim - Chegada : -1;
    public long TempoEspera => EsperaAcumulada;
    public long TempoResposta => PrimeiraCpu >= 0 ? PrimeiraCpu - Chegada : -1;
}

/// <summary>Métricas agregadas de um processo.</summary>
public sealed class MetricasProcesso
{
    public int Id { get; init; }
    public long Chegada { get; set; }
    public long Fim { get; set; } = -1;
    public long TempoRetorno => Fim >= 0 ? Fim - Chegada : -1;
}

/// <summary>
/// Coleta e calcula as métricas da simulação a partir do clock lógico. As
/// definições são aplicadas de maneira uniforme a todas as políticas.
/// </summary>
public sealed class ColetorMetricas
{
    private readonly Dictionary<ThreadSimulada, MetricasThread> _threads = new();
    private readonly Dictionary<int, MetricasProcesso> _processos = new();

    public long UnidadesCpuUteis { get; private set; }
    public long TempoOverheadTroca { get; private set; }
    public long NumeroTrocasContexto { get; private set; }
    public long TotalReferencias { get; private set; }
    public long Acertos { get; private set; }
    public long Faltas { get; private set; }
    public long Carregamentos { get; private set; }
    public long Substituicoes { get; private set; }
    public long TempoFinal { get; private set; }
    public int ProcessosConcluidos { get; private set; }

    public IReadOnlyDictionary<ThreadSimulada, MetricasThread> Threads => _threads;
    public IReadOnlyDictionary<int, MetricasProcesso> Processos => _processos;

    public void RegistrarThread(ThreadSimulada t)
        => _threads[t] = new MetricasThread { Identificacao = t.Identificacao, Chegada = t.Chegada };

    public void RegistrarProcesso(Processo p)
        => _processos[p.Id] = new MetricasProcesso { Id = p.Id, Chegada = p.Chegada };

    public void EntrarPronto(ThreadSimulada t, long clock)
    {
        var m = _threads[t];
        if (m.EntradaProntoEm < 0) m.EntradaProntoEm = clock;
    }

    public void SairPronto(ThreadSimulada t, long clock)
    {
        var m = _threads[t];
        if (m.EntradaProntoEm >= 0)
        {
            m.EsperaAcumulada += clock - m.EntradaProntoEm;
            m.EntradaProntoEm = -1;
        }
    }

    public void RegistrarResposta(ThreadSimulada t, long clock)
    {
        var m = _threads[t];
        if (m.PrimeiraCpu < 0) m.PrimeiraCpu = clock;
    }

    public void FinalizarThread(ThreadSimulada t, long clock) => _threads[t].Fim = clock;

    public void FinalizarProcesso(Processo p, long clock)
    {
        _processos[p.Id].Fim = clock;
        ProcessosConcluidos++;
    }

    public void RegistrarUsoCpu(long unidades) => UnidadesCpuUteis += unidades;
    public void RegistrarTrocaContexto(long custo) { NumeroTrocasContexto++; TempoOverheadTroca += custo; }

    public void RegistrarReferencia() => TotalReferencias++;
    public void RegistrarAcerto() => Acertos++;
    public void RegistrarFalta() => Faltas++;
    public void RegistrarCarregamento() => Carregamentos++;
    public void RegistrarSubstituicao() => Substituicoes++;

    public void DefinirTempoFinal(long clock) => TempoFinal = clock;

    // --- Derivadas ---
    public double UtilizacaoCpu => TempoFinal > 0 ? (double)UnidadesCpuUteis / TempoFinal : 0;
    public double FracaoOverhead => TempoFinal > 0 ? (double)TempoOverheadTroca / TempoFinal : 0;
    public double Throughput => TempoFinal > 0 ? (double)ProcessosConcluidos / TempoFinal : 0;
    public double TaxaFaltas => TotalReferencias > 0 ? (double)Faltas / TotalReferencias : 0;
}
