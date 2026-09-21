using SimuladorSO.Nucleo.Comum;

namespace SimuladorSO.Nucleo.Processos;

/// <summary>
/// Processo do modelo (não confundir com processos reais do hospedeiro).
/// Mantém recursos e informações de execução por meio do seu PCB e agrega threads.
/// </summary>
public sealed class Processo
{
    public Pcb Pcb { get; }
    public long Chegada { get; }
    public IReadOnlyList<ThreadSimulada> Threads => Pcb.Threads;

    public Processo(int id, int prioridade, long chegada)
    {
        Chegada = chegada;
        Pcb = new Pcb(id, prioridade, chegada);
    }

    public int Id => Pcb.Id;
    public int Prioridade => Pcb.Prioridade;

    public bool TodasThreadsFinalizadas()
        => Pcb.Threads.All(t => t.Estado == EstadoThread.Finalizada);
}

/// <summary>
/// Thread de usuário: a unidade que disputa a CPU. Mantém o cursor de execução
/// sobre sua sequência de operações e o contexto de escalonamento.
/// </summary>
public sealed class ThreadSimulada
{
    public Tcb Tcb { get; }
    public Processo Processo => Tcb.Processo;
    public IReadOnlyList<IOperacao> Operacoes { get; }
    public long Chegada { get; }

    // --- Cursor de execução ---
    /// <summary>Índice da operação atual na sequência.</summary>
    public int IndiceOperacao { get; set; }
    /// <summary>Passos já executados no surto de CPU atual.</summary>
    public int PassosExecutadosNoSurto { get; set; }
    /// <summary>Unidades de quantum já consumidas desde o último despacho.</summary>
    public long QuantumConsumido { get; set; }
    /// <summary>Indica que a thread está retomando o passo que sofreu falta de página.</summary>
    public bool RetomandoAposFalta { get; set; }

    // --- Prioridade dinâmica (para envelhecimento) ---
    public int PrioridadeBase { get; }
    public int PrioridadeEfetiva { get; set; }

    public EstadoThread Estado
    {
        get => Tcb.Estado;
        set => Tcb.Estado = value;
    }

    public string Id => Tcb.Id;

    public ThreadSimulada(Tcb tcb, IReadOnlyList<IOperacao> operacoes, long chegada, int prioridade)
    {
        Tcb = tcb;
        Operacoes = operacoes;
        Chegada = chegada;
        PrioridadeBase = prioridade;
        PrioridadeEfetiva = prioridade;
    }

    public IOperacao? OperacaoAtual =>
        IndiceOperacao < Operacoes.Count ? Operacoes[IndiceOperacao] : null;

    public string Identificacao => $"{Processo.Id}:{Id}";
}
