using SimuladorSO.Nucleo.EntradaSaida;
using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Nucleo;

/// <summary>
/// Um evento discreto da simulação. Cada evento é processado quando o clock lógico
/// atinge seu instante. A ordenação temporal é responsabilidade da fila de eventos.
/// </summary>
public interface IEvento
{
    long Tempo { get; }
    long Sequencia { get; set; }
    void Executar(NucleoSimulacao nucleo);
}

/// <summary>
/// Fila de eventos ordenada por (tempo, sequência). A sequência é um contador
/// monotônico atribuído na inserção, o que garante desempate determinístico entre
/// eventos com o mesmo instante — condição essencial para reprodutibilidade.
/// </summary>
public sealed class FilaDeEventos
{
    private readonly PriorityQueue<IEvento, (long tempo, long seq)> _fila = new();
    private long _proximaSequencia;

    public int Quantidade => _fila.Count;

    public void Agendar(IEvento evento)
    {
        evento.Sequencia = _proximaSequencia++;
        _fila.Enqueue(evento, (evento.Tempo, evento.Sequencia));
    }

    public IEvento Desenfileirar() => _fila.Dequeue();

    public bool Vazia => _fila.Count == 0;
}

// ---------- Eventos concretos ----------

public sealed class EventoChegada : IEvento
{
    public long Tempo { get; }
    public long Sequencia { get; set; }
    public Processo Processo { get; }
    public EventoChegada(long tempo, Processo processo) { Tempo = tempo; Processo = processo; }
    public void Executar(NucleoSimulacao n) => n.ProcessarChegada(Processo);
}

public sealed class EventoDespacho : IEvento
{
    public long Tempo { get; }
    public long Sequencia { get; set; }
    public ThreadSimulada Thread { get; }
    public EventoDespacho(long tempo, ThreadSimulada thread) { Tempo = tempo; Thread = thread; }
    public void Executar(NucleoSimulacao n) => n.ProcessarDespacho(Thread);
}

public sealed class EventoExecucaoCpu : IEvento
{
    public long Tempo { get; }
    public long Sequencia { get; set; }
    public ThreadSimulada Thread { get; }
    public EventoExecucaoCpu(long tempo, ThreadSimulada thread) { Tempo = tempo; Thread = thread; }
    public void Executar(NucleoSimulacao n) => n.ProcessarExecucaoCpu(Thread);
}

public sealed class EventoFimAtendimentoFalta : IEvento
{
    public long Tempo { get; }
    public long Sequencia { get; set; }
    public ThreadSimulada Thread { get; }
    public long Endereco { get; }
    public EventoFimAtendimentoFalta(long tempo, ThreadSimulada thread, long endereco)
    { Tempo = tempo; Thread = thread; Endereco = endereco; }
    public void Executar(NucleoSimulacao n) => n.ProcessarFimAtendimentoFalta(Thread, Endereco);
}

public sealed class EventoConclusaoES : IEvento
{
    public long Tempo { get; }
    public long Sequencia { get; set; }
    public Dispositivo Dispositivo { get; }
    public RequisicaoES Requisicao { get; }
    public EventoConclusaoES(long tempo, Dispositivo dispositivo, RequisicaoES requisicao)
    { Tempo = tempo; Dispositivo = dispositivo; Requisicao = requisicao; }
    public void Executar(NucleoSimulacao n) => n.ProcessarConclusaoES(Dispositivo, Requisicao);
}
