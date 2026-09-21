using SimuladorSO.Nucleo.Comum;

namespace SimuladorSO.Cli;

public sealed class OpcoesExecucao
{
    public string Carga { get; set; } = "";
    public string? ArquivoLog { get; set; }
    public bool MostrarLog { get; set; } = true;
    public Configuracao Configuracao { get; set; } = new();
}

/// <summary>Erro de uso da linha de comando.</summary>
public sealed class ErroArgumentos : Exception
{
    public ErroArgumentos(string mensagem) : base(mensagem) { }
}

/// <summary>
/// Interpreta os argumentos de linha de comando e produz as opções de execução.
/// Parâmetros inválidos produzem mensagem objetiva (via <see cref="ErroArgumentos"/>).
/// </summary>
public static class AnalisadorArgumentos
{
    public static OpcoesExecucao Analisar(string[] args)
    {
        var opc = new OpcoesExecucao();
        string carga = "";
        string? log = null;
        bool mostrarLog = true;

        // Valores de configuração (com padrões).
        var c = new Configuracao();
        PoliticaEscalonamento esc = c.Escalonamento;
        long quantum = c.Quantum, custo = c.CustoTrocaContexto, tamPag = c.TamanhoPagina;
        int molduras = c.NumeroMolduras;
        long tempoFalta = c.TempoAtendimentoFalta, tempoDisp = c.TempoServicoDispositivoPadrao;
        PoliticaSubstituicao sub = c.Substituicao;
        PoliticaDisco disco = c.EscalonamentoDisco;
        int semente = c.Semente;
        bool preempt = c.PrioridadePreemptiva, aging = c.EnvelhecimentoAtivo;
        long intervaloAging = c.IntervaloEnvelhecimento;
        int totalBlocos = c.TotalBlocosDisco;
        long tamBloco = c.TamanhoBloco;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];
            string Prox(string nome)
            {
                if (i + 1 >= args.Length) throw new ErroArgumentos($"Faltou valor para {nome}.");
                return args[++i];
            }
            long ProxLong(string nome) => long.TryParse(Prox(nome), out var v)
                ? v : throw new ErroArgumentos($"Valor numerico invalido para {nome}.");
            int ProxInt(string nome) => int.TryParse(Prox(nome), out var v)
                ? v : throw new ErroArgumentos($"Valor inteiro invalido para {nome}.");

            switch (a)
            {
                case "--carga": carga = Prox(a); break;
                case "--escalonador":
                    esc = Prox(a).ToLowerInvariant() switch
                    {
                        "fcfs" => PoliticaEscalonamento.Fcfs,
                        "rr" or "roundrobin" => PoliticaEscalonamento.RoundRobin,
                        "prioridade" => PoliticaEscalonamento.Prioridade,
                        var x => throw new ErroArgumentos($"Escalonador desconhecido: {x}")
                    };
                    break;
                case "--quantum": quantum = ProxLong(a); break;
                case "--custo-troca": custo = ProxLong(a); break;
                case "--tamanho-pagina": tamPag = ProxLong(a); break;
                case "--molduras": molduras = ProxInt(a); break;
                case "--tempo-falta": tempoFalta = ProxLong(a); break;
                case "--tempo-dispositivo": tempoDisp = ProxLong(a); break;
                case "--substituicao":
                    sub = Prox(a).ToLowerInvariant() switch
                    {
                        "fifo" => PoliticaSubstituicao.Fifo,
                        "lru" => PoliticaSubstituicao.Lru,
                        var x => throw new ErroArgumentos($"Politica de substituicao desconhecida: {x}")
                    };
                    break;
                case "--escalonador-disco":
                    disco = Prox(a).ToLowerInvariant() switch
                    {
                        "fcfs" => PoliticaDisco.Fcfs,
                        "sstf" => PoliticaDisco.Sstf,
                        "scan" => PoliticaDisco.Scan,
                        var x => throw new ErroArgumentos($"Escalonamento de disco desconhecido: {x}")
                    };
                    break;
                case "--semente": semente = ProxInt(a); break;
                case "--nao-preemptivo": preempt = false; break;
                case "--preemptivo": preempt = true; break;
                case "--envelhecimento": aging = true; break;
                case "--intervalo-envelhecimento": intervaloAging = ProxLong(a); break;
                case "--total-blocos": totalBlocos = ProxInt(a); break;
                case "--tamanho-bloco": tamBloco = ProxLong(a); break;
                case "--log": log = Prox(a); break;
                case "--sem-log": mostrarLog = false; break;
                case "-i": case "--interativo":
                    throw new ErroArgumentos("__INTERATIVO__");
                case "--listar-cargas":
                    throw new ErroArgumentos("__LISTAR_CARGAS__");
                case "-h": case "--ajuda": case "--help":
                    throw new ErroArgumentos("__AJUDA__");
                default:
                    throw new ErroArgumentos($"Argumento desconhecido: {a}");
            }
        }

        if (string.IsNullOrWhiteSpace(carga))
            throw new ErroArgumentos(
                "Parametro obrigatorio ausente: --carga <arquivo>." + Environment.NewLine +
                "Dica: execute sem argumentos para usar o modo guiado (menu)." + Environment.NewLine +
                "Cargas prontas disponiveis:" + Environment.NewLine +
                LocalizadorDeCargas.TextoDisponiveis());

        // Aceita atalhos: "mista", "mista.txt" ou "cargas/mista.txt".
        var resolvida = LocalizadorDeCargas.Resolver(carga);
        if (resolvida == null)
            throw new ErroArgumentos(
                $"Carga nao encontrada: {carga}" + Environment.NewLine +
                "Cargas prontas disponiveis:" + Environment.NewLine +
                LocalizadorDeCargas.TextoDisponiveis());
        carga = resolvida;

        if (quantum <= 0) throw new ErroArgumentos("--quantum deve ser positivo.");
        if (tamPag <= 0) throw new ErroArgumentos("--tamanho-pagina deve ser positivo.");
        if (molduras <= 0) throw new ErroArgumentos("--molduras deve ser positivo.");
        if (custo < 0) throw new ErroArgumentos("--custo-troca nao pode ser negativo.");

        opc.Carga = carga;
        opc.ArquivoLog = log;
        opc.MostrarLog = mostrarLog;
        opc.Configuracao = new Configuracao
        {
            Escalonamento = esc,
            Quantum = quantum,
            CustoTrocaContexto = custo,
            PrioridadePreemptiva = preempt,
            EnvelhecimentoAtivo = aging,
            IntervaloEnvelhecimento = intervaloAging,
            TamanhoPagina = tamPag,
            NumeroMolduras = molduras,
            TempoAtendimentoFalta = tempoFalta,
            Substituicao = sub,
            TempoServicoDispositivoPadrao = tempoDisp,
            EscalonamentoDisco = disco,
            TotalBlocosDisco = totalBlocos,
            TamanhoBloco = tamBloco,
            Semente = semente
        };
        return opc;
    }

    public static string TextoAjuda() =>
        """
        Simulador didatico de sistemas operacionais

        COMECE POR AQUI
          Execute sem nenhum argumento para abrir o modo guiado (menu):
            dotnet run --project src/SimuladorSO.Cli -c Release

        Uso:
          simulador [--carga <arquivo>] [opcoes]
          simulador -i                      Modo guiado (menu de perguntas)
          simulador --listar-cargas         Mostra as cargas prontas

        A carga aceita atalho: "--carga mista" equivale a "cargas/mista.txt".

        Exemplos:
          simulador --carga mista
          simulador --carga mista --escalonador rr --quantum 2
          simulador --carga cpu_bound --escalonador prioridade --nao-preemptivo
          simulador --carga padrao_memoria --tamanho-pagina 1 --molduras 3 --substituicao lru
          simulador --carga io_disco --escalonador-disco sstf --sem-log
          simulador --carga mista --log saida.log

        Opcoes de escalonamento:
          --escalonador fcfs|rr|prioridade   Politica de CPU (padrao: fcfs)
          --quantum <n>                      Quantum do Round Robin (padrao: 4)
          --custo-troca <n>                  Custo da troca de contexto (padrao: 1)
          --preemptivo | --nao-preemptivo    Prioridades preemptivas (padrao: preemptivo)
          --envelhecimento                   Ativa aging nas prioridades (extensao)
          --intervalo-envelhecimento <n>     Intervalo do aging (padrao: 5)

        Opcoes de memoria:
          --tamanho-pagina <n>               Tamanho da pagina (padrao: 4)
          --molduras <n>                     Numero de molduras (padrao: 8)
          --tempo-falta <n>                  Tempo de atendimento de falta (padrao: 6)
          --substituicao fifo|lru            Politica de substituicao (padrao: fifo)

        Opcoes de E/S e arquivos:
          --tempo-dispositivo <n>            Tempo de servico padrao (padrao: 5)
          --escalonador-disco fcfs|sstf|scan Politica de disco (padrao: fcfs)
          --total-blocos <n>                 Blocos do disco simulado (padrao: 256)
          --tamanho-bloco <n>                Tamanho do bloco (padrao: 512)

        Gerais:
          --semente <n>                      Semente de aleatoriedade (padrao: 0)
          --log <arquivo>                    Grava o log em arquivo
          --sem-log                          Nao imprime o log (apenas metricas)
          -i, --interativo                   Modo guiado por menu
          --listar-cargas                    Lista as cargas de trabalho prontas
          -h, --ajuda                        Mostra esta ajuda
        """;
}
