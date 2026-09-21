using SimuladorSO.Nucleo.Carga;
using SimuladorSO.Nucleo.Comum;
using SimuladorSO.Nucleo.Log;
using SimuladorSO.Nucleo.Metricas;
using SimuladorSO.Nucleo.Nucleo;

namespace SimuladorSO.Testes;

public static class TestesIntegracao
{
    private static (ColetorMetricas m, string log) Rodar(string carga, Configuracao config)
    {
        var c = LeitorDeCarga.LerTexto(carga);
        var sw = new StringWriter();
        var log = new RegistradorEventos(sw, habilitado: true);
        var nucleo = FabricaSimulador.Construir(config, c, log);
        var m = nucleo.Executar();
        return (m, sw.ToString());
    }

    public static void Executar()
    {
        MetricasFcfsConferidasManualmente();
        TransicoesDeEstadoEBloqueioES();
        TerminoDeQuantum();
        Determinismo();
    }

    // Cenario identico a cargas/verificacao_pequena.txt, valores conferidos a mao.
    private static void MetricasFcfsConferidasManualmente()
    {
        Verificador.DefinirContexto("Integracao/FCFS-manual");
        var carga = """
            proc P1 chegada=0 prioridade=1
            thread T1
              cpu dur=3
              io dev=disco dur=4 pos=0
              cpu dur=2
            proc P2 chegada=1 prioridade=2
            thread T1
              cpu dur=4
            """;
        var cfg = new Configuracao { Escalonamento = PoliticaEscalonamento.Fcfs, CustoTrocaContexto = 0 };
        var (m, _) = Rodar(carga, cfg);

        Verificador.Igual(9L, m.TempoFinal, "tempo total simulado");
        Verificador.Igual(2, m.ProcessosConcluidos, "dois processos concluidos");
        Verificador.Igual(9L, m.UnidadesCpuUteis, "unidades de CPU uteis (3+4+2)");
        Verificador.Igual(3L, m.NumeroTrocasContexto, "tres despachos");

        var p2 = m.Threads.Values.First(t => t.Identificacao == "2:T1");
        Verificador.Igual(2L, p2.TempoResposta, "P2 resposta = 3-1");
        Verificador.Igual(2L, p2.TempoEspera, "P2 espera na fila de prontos");
        Verificador.Igual(6L, p2.TempoRetorno, "P2 retorno = 7-1");

        var p1 = m.Threads.Values.First(t => t.Identificacao == "1:T1");
        Verificador.Igual(9L, p1.TempoRetorno, "P1 retorno = 9-0");
        Verificador.Igual(0L, p1.TempoEspera, "P1 nunca espera na fila");
    }

    private static void TransicoesDeEstadoEBloqueioES()
    {
        Verificador.DefinirContexto("Integracao/Bloqueio-ES");
        var carga = """
            proc P1 chegada=0 prioridade=1
            thread T1
              cpu dur=2
              io dev=disco dur=5 pos=0
              cpu dur=2
            """;
        var cfg = new Configuracao { Escalonamento = PoliticaEscalonamento.Fcfs, CustoTrocaContexto = 0 };
        var (m, log) = Rodar(carga, cfg);

        Verificador.Verdadeiro(log.Contains("Bloqueio"), "log registra bloqueio por E/S");
        Verificador.Verdadeiro(log.Contains("ConclusaoES"), "log registra conclusao de E/S (interrupcao)");
        Verificador.Verdadeiro(log.Contains("Interrupcao"), "log registra desbloqueio via interrupcao");
        Verificador.Verdadeiro(log.Contains("FimThread"), "log registra finalizacao da thread");
        // Executa 2 [0,2], bloqueia [2,7], executa 2 [7,9] -> fim 9
        Verificador.Igual(9L, m.TempoFinal, "tempo total com E/S bloqueante");
        Verificador.Igual(4L, m.UnidadesCpuUteis, "CPU util = 2+2 (E/S nao consome CPU)");
    }

    private static void TerminoDeQuantum()
    {
        Verificador.DefinirContexto("Integracao/Quantum");
        var carga = """
            proc P1 chegada=0 prioridade=1
            thread T1
              cpu dur=10
            """;
        var cfg = new Configuracao
        {
            Escalonamento = PoliticaEscalonamento.RoundRobin,
            Quantum = 3,
            CustoTrocaContexto = 0
        };
        var (_, log) = Rodar(carga, cfg);
        int preempcoes = log.Split('\n').Count(l => l.Contains("Preempcao"));
        // Surto 10, quantum 3 -> fatias 3,3,3,1 -> 3 preempcoes
        Verificador.Igual(3, preempcoes, "numero de preempcoes por fim de quantum");
    }

    private static void Determinismo()
    {
        Verificador.DefinirContexto("Integracao/Determinismo");
        var carga = """
            proc P1 chegada=0 prioridade=1
            thread T1
              cpu dur=4 refs=0,4,8,0
              io dev=disco dur=5 pos=10
              cpu dur=3
            proc P2 chegada=1 prioridade=2
            thread T1
              cpu dur=6 refs=12,16,12
            """;
        var cfg = new Configuracao
        {
            Escalonamento = PoliticaEscalonamento.RoundRobin,
            Quantum = 2,
            NumeroMolduras = 2
        };
        var (m1, log1) = Rodar(carga, cfg);
        var (m2, log2) = Rodar(carga, cfg);
        Verificador.Verdadeiro(log1 == log2, "logs identicos entre execucoes iguais");
        Verificador.Igual(m1.TempoFinal, m2.TempoFinal, "tempo final identico");
        Verificador.Igual(m1.Faltas, m2.Faltas, "faltas de pagina identicas");
    }
}
