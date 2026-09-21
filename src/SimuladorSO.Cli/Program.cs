using SimuladorSO.Cli;
using SimuladorSO.Nucleo.Carga;
using SimuladorSO.Nucleo.Comum;
using SimuladorSO.Nucleo.Log;
using SimuladorSO.Nucleo.Metricas;
using SimuladorSO.Nucleo.Nucleo;

// Ponto de entrada. Há dois modos:
//  - Interativo (sem argumentos, em terminal): abre o menu principal, que fica
//    em laço até o usuário escolher "Encerrar".
//  - Direto (com argumentos): executa uma simulação e encerra, o que mantém o
//    programa utilizável em scripts e na automação dos experimentos.
try
{
    bool interativo = false;
    OpcoesExecucao? opcoes = null;

    if (args.Length == 0 && !Console.IsInputRedirected)
    {
        interativo = true;
    }
    else if (args.Length == 0)
    {
        // Entrada redirecionada (pipe/CI): sem menu, apenas a ajuda.
        Console.WriteLine(AnalisadorArgumentos.TextoAjuda());
        return 0;
    }
    else
    {
        try
        {
            opcoes = AnalisadorArgumentos.Analisar(args);
        }
        catch (ErroArgumentos e)
        {
            switch (e.Message)
            {
                case "__AJUDA__":
                    Console.WriteLine(AnalisadorArgumentos.TextoAjuda());
                    return 0;
                case "__LISTAR_CARGAS__":
                    Console.WriteLine("Cargas de trabalho disponiveis:");
                    Console.WriteLine(LocalizadorDeCargas.TextoDisponiveis());
                    Console.WriteLine();
                    Console.WriteLine("Use, por exemplo: --carga mista");
                    return 0;
                case "__INTERATIVO__":
                    interativo = true;
                    break;
                default:
                    Console.Error.WriteLine($"Erro de argumentos: {e.Message}");
                    Console.Error.WriteLine("Use --ajuda para ver as opcoes ou execute sem argumentos para o modo guiado.");
                    return 2;
            }
        }
    }

    // ---------------- Modo interativo: laço do menu principal ----------------
    if (interativo)
    {
        MenuInterativo.Boas_Vindas();

        while (true)
        {
            var escolha = MenuInterativo.MenuPrincipal();

            switch (escolha)
            {
                case AcaoMenu.Encerrar:
                    Console.WriteLine();
                    Console.WriteLine("Encerrando o simulador. Ate logo!");
                    return 0;

                case AcaoMenu.ListarCargas:
                    Console.WriteLine();
                    Console.WriteLine("Cargas de trabalho disponiveis:");
                    Console.WriteLine(LocalizadorDeCargas.TextoDisponiveis());
                    MenuInterativo.Pausar();
                    break;

                case AcaoMenu.Ajuda:
                    Console.WriteLine();
                    Console.WriteLine(AnalisadorArgumentos.TextoAjuda());
                    MenuInterativo.Pausar();
                    break;

                case AcaoMenu.Simular:
                    var config = MenuInterativo.MontarExecucao();
                    if (config == null) break;          // usuário voltou ao menu
                    ExecutarSimulacao(config);
                    MenuInterativo.Pausar();
                    break;
            }
        }
    }

    // ---------------- Modo direto: uma execução e sai ----------------
    return ExecutarSimulacao(opcoes!);
}
catch (Exception e)
{
    Console.Error.WriteLine($"Erro inesperado: {e.Message}");
    return 1;
}

// Executa uma simulação completa e imprime o relatório de métricas.
// Retorna o código de saída (0 = sucesso, 3 = carga inválida).
static int ExecutarSimulacao(OpcoesExecucao opcoes)
{
    Carga carga;
    try
    {
        carga = LeitorDeCarga.LerArquivo(opcoes.Carga);
    }
    catch (ExcecaoCarga e)
    {
        Console.Error.WriteLine($"Erro na carga de trabalho: {e.Message}");
        return 3;
    }

    // Semente da carga sobrescreve a da linha de comando, se presente.
    var config = opcoes.Configuracao;
    if (carga.Semente.HasValue)
        config = CopiarComSemente(config, carga.Semente.Value);

    // Destino do log: arquivo, console ou descartado.
    TextWriter destinoLog;
    StreamWriter? arquivoLog = null;
    if (opcoes.ArquivoLog != null)
    {
        arquivoLog = new StreamWriter(opcoes.ArquivoLog, append: false);
        destinoLog = arquivoLog;
    }
    else
    {
        destinoLog = Console.Out;
    }

    var registrador = new RegistradorEventos(destinoLog, habilitado: opcoes.MostrarLog || opcoes.ArquivoLog != null);

    var nucleo = FabricaSimulador.Construir(config, carga, registrador);
    var metricas = nucleo.Executar();

    arquivoLog?.Flush();
    arquivoLog?.Dispose();

    if (opcoes.ArquivoLog != null)
        Console.WriteLine($"Log de eventos gravado em: {opcoes.ArquivoLog}");

    // Métricas sempre no console.
    Console.WriteLine(RelatorioMetricas.Gerar(metricas, nucleo.Dispositivos));
    return 0;
}

static Configuracao CopiarComSemente(Configuracao c, int semente) => new()
{
    Escalonamento = c.Escalonamento,
    Quantum = c.Quantum,
    CustoTrocaContexto = c.CustoTrocaContexto,
    PrioridadePreemptiva = c.PrioridadePreemptiva,
    EnvelhecimentoAtivo = c.EnvelhecimentoAtivo,
    IntervaloEnvelhecimento = c.IntervaloEnvelhecimento,
    TamanhoPagina = c.TamanhoPagina,
    NumeroMolduras = c.NumeroMolduras,
    TempoAtendimentoFalta = c.TempoAtendimentoFalta,
    Substituicao = c.Substituicao,
    TempoServicoDispositivoPadrao = c.TempoServicoDispositivoPadrao,
    EscalonamentoDisco = c.EscalonamentoDisco,
    TotalBlocosDisco = c.TotalBlocosDisco,
    TamanhoBloco = c.TamanhoBloco,
    Semente = semente
};
