using SimuladorSO.Nucleo.Comum;

namespace SimuladorSO.Cli;

/// <summary>Ações possíveis no menu principal do modo interativo.</summary>
public enum AcaoMenu
{
    Simular,
    ListarCargas,
    Ajuda,
    Encerrar
}

/// <summary>
/// Modo interativo: faz perguntas simples, com valores padrão entre colchetes
/// (basta pressionar Enter para aceitar). Evita que o usuário precise decorar
/// os parâmetros de linha de comando.
/// </summary>
public static class MenuInterativo
{
    /// <summary>Mensagem de abertura, mostrada uma única vez.</summary>
    public static void Boas_Vindas()
    {
        Console.WriteLine();
        Console.WriteLine("==============================================================");
        Console.WriteLine("   Simulador Didatico de Sistemas Operacionais - modo guiado");
        Console.WriteLine("==============================================================");
        Console.WriteLine("Pressione Enter para aceitar o valor padrao [entre colchetes].");
    }

    /// <summary>
    /// Menu principal. O programa permanece neste laço até o usuário escolher
    /// explicitamente a opção "Encerrar".
    /// </summary>
    public static AcaoMenu MenuPrincipal()
    {
        Console.WriteLine();
        Console.WriteLine("=================== MENU PRINCIPAL ===================");
        Console.WriteLine("  [1] Executar uma simulacao");
        Console.WriteLine("  [2] Listar cargas de trabalho disponiveis");
        Console.WriteLine("  [3] Ver ajuda e parametros de linha de comando");
        Console.WriteLine("  [0] Encerrar");
        Console.WriteLine("======================================================");

        while (true)
        {
            Console.Write("Escolha uma opcao [1]: ");
            var linha = Console.ReadLine();

            // Ctrl+D / fim de entrada: encerra com segurança.
            if (linha == null) return AcaoMenu.Encerrar;

            var texto = linha.Trim();
            if (texto.Length == 0) return AcaoMenu.Simular;   // Enter = opção padrão

            switch (texto.ToLowerInvariant())
            {
                case "1": return AcaoMenu.Simular;
                case "2": return AcaoMenu.ListarCargas;
                case "3": return AcaoMenu.Ajuda;
                case "0":
                case "sair":
                case "encerrar":
                case "q": return AcaoMenu.Encerrar;
                default:
                    Console.WriteLine("  Opcao invalida. Digite 1, 2, 3 ou 0.");
                    break;
            }
        }
    }

    /// <summary>Aguarda o usuário antes de voltar ao menu principal.</summary>
    public static void Pausar()
    {
        Console.WriteLine();
        Console.Write("Pressione Enter para voltar ao menu principal...");
        Console.ReadLine();
    }

    /// <summary>
    /// Conduz as perguntas que montam uma execução. Retorna <c>null</c> se o
    /// usuário optar por voltar ao menu principal.
    /// </summary>
    public static OpcoesExecucao? MontarExecucao()
    {
        Console.WriteLine();
        var carga = EscolherCarga();
        if (carga == null) return null;

        // Parte dos padrões e vai ajustando conforme as respostas; a
        // Configuracao é imutável, então é construída de uma vez no final.
        var padroes = new Configuracao();
        long quantum = padroes.Quantum, custo = padroes.CustoTrocaContexto;
        long tamPagina = padroes.TamanhoPagina;
        int molduras = padroes.NumeroMolduras;
        bool preemptiva = padroes.PrioridadePreemptiva;
        var substituicao = padroes.Substituicao;
        var disco = padroes.EscalonamentoDisco;

        // --- Escalonador ---
        var esc = Escolher("Politica de escalonamento da CPU", new[]
        {
            ("FCFS (ordem de chegada)", PoliticaEscalonamento.Fcfs),
            ("Round Robin (fatias de tempo)", PoliticaEscalonamento.RoundRobin),
            ("Prioridade", PoliticaEscalonamento.Prioridade),
        }, padrao: 0);

        if (esc == PoliticaEscalonamento.RoundRobin)
            quantum = PerguntarInteiro("Quantum (unidades de tempo)", (int)quantum, min: 1);

        if (esc == PoliticaEscalonamento.Prioridade)
            preemptiva = PerguntarSimNao("Prioridade preemptiva?", padrao: true);

        // --- Ajustes opcionais ---
        Console.WriteLine();
        if (PerguntarSimNao("Ajustar memoria, E/S e custo de troca?", padrao: false))
        {
            Console.WriteLine();
            custo = PerguntarInteiro("Custo da troca de contexto", (int)custo, min: 0);
            tamPagina = PerguntarInteiro("Tamanho da pagina", (int)tamPagina, min: 1);
            molduras = PerguntarInteiro("Numero de molduras", molduras, min: 1);
            substituicao = Escolher("Substituicao de paginas", new[]
            {
                ("FIFO", PoliticaSubstituicao.Fifo),
                ("LRU", PoliticaSubstituicao.Lru),
            }, padrao: 0);
            disco = Escolher("Escalonamento do disco", new[]
            {
                ("FCFS", PoliticaDisco.Fcfs),
                ("SSTF", PoliticaDisco.Sstf),
                ("SCAN (elevador)", PoliticaDisco.Scan),
            }, padrao: 0);
        }

        var opcoes = new OpcoesExecucao
        {
            Carga = carga,
            Configuracao = new Configuracao
            {
                Escalonamento = esc,
                Quantum = quantum,
                CustoTrocaContexto = custo,
                PrioridadePreemptiva = preemptiva,
                TamanhoPagina = tamPagina,
                NumeroMolduras = molduras,
                Substituicao = substituicao,
                EscalonamentoDisco = disco,
            }
        };

        // --- Log ---
        Console.WriteLine();
        var destino = Escolher("O que fazer com o log de eventos", new[]
        {
            ("Mostrar na tela (junto com as metricas)", 0),
            ("Salvar em arquivo (so metricas na tela)", 1),
            ("Nao gerar log (apenas metricas)", 2),
        }, padrao: 0);

        if (destino == 1)
        {
            var nome = Perguntar("Nome do arquivo de log", "saida.log");
            opcoes.ArquivoLog = string.IsNullOrWhiteSpace(nome) ? "saida.log" : nome.Trim();
            opcoes.MostrarLog = false;
        }
        else
        {
            opcoes.MostrarLog = destino == 0;
        }

        MostrarComandoEquivalente(opcoes);
        return opcoes;
    }

    private static string? EscolherCarga()
    {
        var arquivos = LocalizadorDeCargas.Disponiveis();
        if (arquivos.Count == 0)
        {
            Console.WriteLine("Nenhuma carga encontrada na pasta 'cargas/'.");
            var caminho = Perguntar("Informe o caminho de um arquivo de carga", "");
            var resolvido = LocalizadorDeCargas.Resolver(caminho);
            if (resolvido == null) Console.WriteLine("Arquivo nao encontrado.");
            return resolvido;
        }

        Console.WriteLine("Escolha a carga de trabalho:");
        for (int i = 0; i < arquivos.Count; i++)
        {
            var nome = Path.GetFileNameWithoutExtension(arquivos[i]);
            var desc = LocalizadorDeCargas.DescricaoDe(arquivos[i]);
            Console.WriteLine($"  [{i + 1}] {nome,-22} {desc}");
        }
        Console.WriteLine("  [0] Voltar ao menu principal");

        int escolha = PerguntarInteiro("Opcao", 1, min: 0, max: arquivos.Count);
        return escolha == 0 ? null : arquivos[escolha - 1];
    }

    private static T Escolher<T>(string titulo, (string rotulo, T valor)[] opcoes, int padrao)
    {
        Console.WriteLine($"{titulo}:");
        for (int i = 0; i < opcoes.Length; i++)
            Console.WriteLine($"  [{i + 1}] {opcoes[i].rotulo}");
        int escolha = PerguntarInteiro("Opcao", padrao + 1, min: 1, max: opcoes.Length);
        return opcoes[escolha - 1].valor;
    }

    private static string Perguntar(string rotulo, string padrao)
    {
        Console.Write(string.IsNullOrEmpty(padrao) ? $"{rotulo}: " : $"{rotulo} [{padrao}]: ");
        var linha = Console.ReadLine();
        return string.IsNullOrWhiteSpace(linha) ? padrao : linha.Trim();
    }

    private static int PerguntarInteiro(string rotulo, int padrao, int min = int.MinValue, int max = int.MaxValue)
    {
        while (true)
        {
            var texto = Perguntar(rotulo, padrao.ToString());
            if (int.TryParse(texto, out var valor) && valor >= min && valor <= max)
                return valor;
            Console.WriteLine($"  Valor invalido. Informe um numero entre {min} e {max}.");
        }
    }

    private static bool PerguntarSimNao(string rotulo, bool padrao)
    {
        while (true)
        {
            var texto = Perguntar($"{rotulo} (s/n)", padrao ? "s" : "n").ToLowerInvariant();
            if (texto is "s" or "sim") return true;
            if (texto is "n" or "nao" or "não") return false;
            Console.WriteLine("  Responda com 's' ou 'n'.");
        }
    }

    /// <summary>
    /// Mostra o comando de linha equivalente às escolhas feitas, ajudando o
    /// usuário a aprender os parâmetros e a repetir a execução depois.
    /// </summary>
    private static void MostrarComandoEquivalente(OpcoesExecucao o)
    {
        var c = o.Configuracao;
        var partes = new List<string> { "dotnet run --project src/SimuladorSO.Cli -c Release --" };
        var nomeCurto = Path.GetFileNameWithoutExtension(o.Carga);
        partes.Add($"--carga {nomeCurto}");

        partes.Add("--escalonador " + c.Escalonamento switch
        {
            PoliticaEscalonamento.RoundRobin => "rr",
            PoliticaEscalonamento.Prioridade => "prioridade",
            _ => "fcfs"
        });

        if (c.Escalonamento == PoliticaEscalonamento.RoundRobin) partes.Add($"--quantum {c.Quantum}");
        if (c.Escalonamento == PoliticaEscalonamento.Prioridade && !c.PrioridadePreemptiva) partes.Add("--nao-preemptivo");
        if (c.CustoTrocaContexto != 1) partes.Add($"--custo-troca {c.CustoTrocaContexto}");
        if (c.TamanhoPagina != 4) partes.Add($"--tamanho-pagina {c.TamanhoPagina}");
        if (c.NumeroMolduras != 8) partes.Add($"--molduras {c.NumeroMolduras}");
        if (c.Substituicao != PoliticaSubstituicao.Fifo) partes.Add("--substituicao lru");
        if (c.EscalonamentoDisco != PoliticaDisco.Fcfs)
            partes.Add("--escalonador-disco " + (c.EscalonamentoDisco == PoliticaDisco.Sstf ? "sstf" : "scan"));
        if (o.ArquivoLog != null) partes.Add($"--log {o.ArquivoLog}");
        else if (!o.MostrarLog) partes.Add("--sem-log");

        Console.WriteLine();
        Console.WriteLine("Comando equivalente (para repetir esta execucao direto no terminal):");
        Console.WriteLine("  " + string.Join(" ", partes));
        Console.WriteLine();
        Console.WriteLine("--------------------------------------------------------------");
        Console.WriteLine();
    }
}
