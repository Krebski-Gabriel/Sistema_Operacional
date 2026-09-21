using SimuladorSO.Nucleo.Escalonamento;
using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Testes;

public static class TestesEscalonamento
{
    private static ThreadSimulada NovaThread(int procId, string tid, int prioridade, long chegada)
    {
        var p = new Processo(procId, prioridade, chegada);
        var tcb = new Tcb(tid, p);
        var t = new ThreadSimulada(tcb, new List<IOperacao> { SurtoCpu.Criar(1) }, chegada, prioridade);
        p.Pcb.Threads.Add(t);
        return t;
    }

    public static void Executar()
    {
        OrdemFcfs();
        RoundRobinUsaQuantum();
        PrioridadeSelecao();
        PrioridadePreempcao();
    }

    private static void OrdemFcfs()
    {
        Verificador.DefinirContexto("Escalonamento/FCFS");
        var e = new EscalonadorFcfs();
        var a = NovaThread(1, "T1", 0, 0);
        var b = NovaThread(2, "T1", 0, 0);
        var c = NovaThread(3, "T1", 0, 0);
        e.Admitir(a, 0); e.Admitir(b, 0); e.Admitir(c, 0);
        Verificador.Igual("1:T1", e.Selecionar(0).Identificacao, "FCFS seleciona por ordem de admissao (1)");
        Verificador.Igual("2:T1", e.Selecionar(0).Identificacao, "FCFS seleciona por ordem de admissao (2)");
        Verificador.Igual("3:T1", e.Selecionar(0).Identificacao, "FCFS seleciona por ordem de admissao (3)");
        Verificador.Falso(e.TemProntas, "fila vazia ao final");
    }

    private static void RoundRobinUsaQuantum()
    {
        Verificador.DefinirContexto("Escalonamento/RR");
        var e = new EscalonadorRoundRobin();
        Verificador.Verdadeiro(e.UsaQuantum, "Round Robin usa quantum");
        Verificador.Falso(e.Preemptivo, "RR nao e preemptivo por prioridade");
    }

    private static void PrioridadeSelecao()
    {
        Verificador.DefinirContexto("Escalonamento/Prioridade");
        var e = new EscalonadorPrioridade(preemptivo: true, envelhecimento: false, intervaloEnvelhecimento: 5);
        var baixa = NovaThread(1, "T1", prioridade: 5, chegada: 0);
        var alta = NovaThread(2, "T1", prioridade: 1, chegada: 0);
        var media = NovaThread(3, "T1", prioridade: 3, chegada: 0);
        e.Admitir(baixa, 0); e.Admitir(alta, 0); e.Admitir(media, 0);
        Verificador.Igual("2:T1", e.Selecionar(0).Identificacao, "prioridade: menor valor primeiro (alta)");
        Verificador.Igual("3:T1", e.Selecionar(0).Identificacao, "prioridade: media em seguida");
        Verificador.Igual("1:T1", e.Selecionar(0).Identificacao, "prioridade: baixa por ultimo");
    }

    private static void PrioridadePreempcao()
    {
        Verificador.DefinirContexto("Escalonamento/Preempcao");
        var e = new EscalonadorPrioridade(preemptivo: true, envelhecimento: false, intervaloEnvelhecimento: 5);
        var emExec = NovaThread(1, "T1", prioridade: 5, chegada: 0);
        var melhor = NovaThread(2, "T1", prioridade: 1, chegada: 1);
        e.Admitir(melhor, 1);
        Verificador.Verdadeiro(e.DevePreemptar(emExec, 1), "thread de prioridade superior deve preemptar");

        var e2 = new EscalonadorPrioridade(preemptivo: false, envelhecimento: false, intervaloEnvelhecimento: 5);
        e2.Admitir(melhor, 1);
        Verificador.Falso(e2.DevePreemptar(emExec, 1), "nao preemptivo nunca preempta");
    }
}
