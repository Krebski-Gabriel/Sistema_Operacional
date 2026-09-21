using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Escalonamento;

/// <summary>
/// First-Come, First-Served. Fila de prontos por ordem de admissão. Não
/// preemptivo e sem quantum. Empates de admissão simultânea resolvidos pela
/// ordem de inserção (determinística).
/// </summary>
public sealed class EscalonadorFcfs : IEscalonador
{
    private readonly Queue<ThreadSimulada> _fila = new();
    public string Nome => "FCFS";
    public bool UsaQuantum => false;
    public bool Preemptivo => false;
    public bool TemProntas => _fila.Count > 0;
    public int Quantidade => _fila.Count;

    public void Admitir(ThreadSimulada thread, long clock) => _fila.Enqueue(thread);
    public ThreadSimulada Selecionar(long clock) => _fila.Dequeue();
    public bool DevePreemptar(ThreadSimulada emExecucao, long clock) => false;
}

/// <summary>
/// Round Robin. Mesma fila FIFO do FCFS; a diferença é o uso de quantum, aplicado
/// pelo núcleo (UsaQuantum = true). A thread preemptada por fim de quantum retorna
/// ao final da fila de prontos.
/// </summary>
public sealed class EscalonadorRoundRobin : IEscalonador
{
    private readonly Queue<ThreadSimulada> _fila = new();
    public string Nome => "RoundRobin";
    public bool UsaQuantum => true;
    public bool Preemptivo => false;
    public bool TemProntas => _fila.Count > 0;
    public int Quantidade => _fila.Count;

    public void Admitir(ThreadSimulada thread, long clock) => _fila.Enqueue(thread);
    public ThreadSimulada Selecionar(long clock) => _fila.Dequeue();
    public bool DevePreemptar(ThreadSimulada emExecucao, long clock) => false;
}
