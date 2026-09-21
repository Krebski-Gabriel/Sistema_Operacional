using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Escalonamento;

/// <summary>
/// Escalonamento por prioridades. Menor valor numérico indica maior prioridade.
/// Pode ser preemptivo ou não preemptivo. Suporta envelhecimento (aging) opcional:
/// threads que esperam muito têm sua prioridade efetiva melhorada, reduzindo a
/// inanição. Empates são resolvidos por ordem de chegada e, então, por identificador.
/// </summary>
public sealed class EscalonadorPrioridade : IEscalonador
{
    private readonly List<ThreadSimulada> _prontas = new();
    private readonly Dictionary<ThreadSimulada, long> _admissao = new();
    private readonly bool _preemptivo;
    private readonly bool _envelhecimento;
    private readonly long _intervaloEnvelhecimento;

    public EscalonadorPrioridade(bool preemptivo, bool envelhecimento, long intervaloEnvelhecimento)
    {
        _preemptivo = preemptivo;
        _envelhecimento = envelhecimento;
        _intervaloEnvelhecimento = Math.Max(1, intervaloEnvelhecimento);
    }

    public string Nome => _preemptivo ? "Prioridade(preemptiva)" : "Prioridade(nao-preemptiva)";
    public bool UsaQuantum => false;
    public bool Preemptivo => _preemptivo;
    public bool TemProntas => _prontas.Count > 0;
    public int Quantidade => _prontas.Count;

    public void Admitir(ThreadSimulada thread, long clock)
    {
        _prontas.Add(thread);
        _admissao[thread] = clock;
        thread.PrioridadeEfetiva = thread.PrioridadeBase;
    }

    public ThreadSimulada Selecionar(long clock)
    {
        AtualizarEnvelhecimento(clock);
        var escolhida = Melhor(clock);
        _prontas.Remove(escolhida);
        _admissao.Remove(escolhida);
        return escolhida;
    }

    public bool DevePreemptar(ThreadSimulada emExecucao, long clock)
    {
        if (!_preemptivo || _prontas.Count == 0) return false;
        AtualizarEnvelhecimento(clock);
        var melhor = Melhor(clock);
        return melhor.PrioridadeEfetiva < emExecucao.PrioridadeEfetiva;
    }

    private ThreadSimulada Melhor(long clock)
    {
        ThreadSimulada? melhor = null;
        foreach (var t in _prontas)
        {
            if (melhor == null || Compara(t, melhor) < 0)
                melhor = t;
        }
        return melhor!;
    }

    // Ordem determinística: prioridade efetiva, depois chegada, depois identificador.
    private static int Compara(ThreadSimulada a, ThreadSimulada b)
    {
        int c = a.PrioridadeEfetiva.CompareTo(b.PrioridadeEfetiva);
        if (c != 0) return c;
        c = a.Chegada.CompareTo(b.Chegada);
        if (c != 0) return c;
        return string.CompareOrdinal(a.Identificacao, b.Identificacao);
    }

    private void AtualizarEnvelhecimento(long clock)
    {
        if (!_envelhecimento) return;
        foreach (var t in _prontas)
        {
            long espera = clock - _admissao[t];
            int melhoria = (int)(espera / _intervaloEnvelhecimento);
            t.PrioridadeEfetiva = Math.Max(0, t.PrioridadeBase - melhoria);
        }
    }
}
