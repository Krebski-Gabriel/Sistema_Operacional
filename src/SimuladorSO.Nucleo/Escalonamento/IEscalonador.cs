using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Escalonamento;

/// <summary>
/// Política de escalonamento de CPU (componente substituível). O núcleo interage
/// apenas por esta interface, sem conhecer o algoritmo concreto. Empates devem ser
/// resolvidos de forma determinística e documentada.
/// </summary>
public interface IEscalonador
{
    string Nome { get; }

    /// <summary>Indica se a política usa fatia de tempo (quantum) — caso do Round Robin.</summary>
    bool UsaQuantum { get; }

    /// <summary>Indica se a política pode preemptar a thread em execução (prioridades preemptivas).</summary>
    bool Preemptivo { get; }

    bool TemProntas { get; }
    int Quantidade { get; }

    /// <summary>Adiciona uma thread à fila de prontos no instante informado.</summary>
    void Admitir(ThreadSimulada thread, long clock);

    /// <summary>Seleciona e remove a próxima thread a executar (pressupõe fila não vazia).</summary>
    ThreadSimulada Selecionar(long clock);

    /// <summary>Indica se alguma thread pronta deve preemptar a que está em execução.</summary>
    bool DevePreemptar(ThreadSimulada emExecucao, long clock);
}
