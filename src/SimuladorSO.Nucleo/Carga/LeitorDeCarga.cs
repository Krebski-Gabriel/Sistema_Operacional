using SimuladorSO.Nucleo.EntradaSaida;
using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Carga;

/// <summary>
/// Leitor do arquivo de carga (formato textual documentado no README). Faz o
/// parsing estatal (o processo/thread corrente define o contexto das operações)
/// e valida o formato, produzindo erros com número de linha.
/// </summary>
public static class LeitorDeCarga
{
    public static Carga LerArquivo(string caminho)
    {
        if (!File.Exists(caminho))
            throw new ExcecaoCarga($"Arquivo de carga nao encontrado: '{caminho}'.");
        return Ler(File.ReadAllLines(caminho));
    }

    public static Carga LerTexto(string texto)
        => Ler(texto.Replace("\r\n", "\n").Split('\n'));

    public static Carga Ler(IReadOnlyList<string> linhas)
    {
        var carga = new Carga();
        Processo? procAtual = null;
        ThreadSimulada? threadAtual = null;
        List<IOperacao>? opsAtual = null;
        int idSequencial = 0;

        for (int n = 0; n < linhas.Count; n++)
        {
            int numLinha = n + 1;
            string linha = RemoverComentario(linhas[n]).Trim();
            if (linha.Length == 0) continue;

            var tokens = linha.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            string dir = tokens[0].ToLowerInvariant();
            var args = ParsearArgumentos(tokens, numLinha);

            switch (dir)
            {
                case "config":
                    if (args.TryGetValue("semente", out var s))
                        carga.Semente = InteiroObrig(s, "semente", numLinha);
                    break;

                case "disp":
                    carga.Dispositivos.Add(LerDispositivo(tokens, args, numLinha));
                    break;

                case "proc":
                {
                    long chegada = LongOpc(args, "chegada", 0, numLinha);
                    int prioridade = (int)LongOpc(args, "prioridade", 0, numLinha);
                    int id = InferirId(tokens.Length > 1 ? tokens[1] : "", ref idSequencial);
                    procAtual = new Processo(id, prioridade, chegada);
                    carga.Processos.Add(procAtual);
                    threadAtual = null;
                    break;
                }

                case "thread":
                {
                    if (procAtual == null)
                        throw new ExcecaoCarga($"Linha {numLinha}: 'thread' sem 'proc' anterior.");
                    if (tokens.Length < 2)
                        throw new ExcecaoCarga($"Linha {numLinha}: 'thread' requer um identificador.");
                    string tid = tokens[1];
                    opsAtual = new List<IOperacao>();
                    var tcb = new Tcb(tid, procAtual);
                    threadAtual = new ThreadSimulada(tcb, opsAtual, procAtual.Chegada, procAtual.Prioridade);
                    procAtual.Pcb.Threads.Add(threadAtual);
                    break;
                }

                case "cpu":
                {
                    ExigirThread(threadAtual, numLinha);
                    long dur = LongOpc(args, "dur", 0, numLinha);
                    var refs = LerReferencias(args, numLinha);
                    if (dur <= 0 && refs.Count == 0)
                        throw new ExcecaoCarga($"Linha {numLinha}: surto de CPU precisa de 'dur' > 0 ou 'refs'.");
                    opsAtual!.Add(SurtoCpu.Criar(dur, refs));
                    break;
                }

                case "io":
                {
                    ExigirThread(threadAtual, numLinha);
                    string dev = TextoObrig(args, "dev", numLinha);
                    long dur = LongOpc(args, "dur", 0, numLinha);
                    long pos = LongOpc(args, "pos", 0, numLinha);
                    bool bloq = LerTipoBloqueio(args, numLinha);
                    opsAtual!.Add(new OperacaoES(dev, dur, bloq, pos));
                    break;
                }

                case "fs":
                {
                    ExigirThread(threadAtual, numLinha);
                    opsAtual!.Add(LerOperacaoArquivo(tokens, args, numLinha));
                    break;
                }

                default:
                    throw new ExcecaoCarga($"Linha {numLinha}: diretiva desconhecida '{dir}'.");
            }
        }

        Validar(carga);
        return carga;
    }

    // ---------- Auxiliares ----------

    private static string RemoverComentario(string linha)
    {
        int i = linha.IndexOf('#');
        return i >= 0 ? linha.Substring(0, i) : linha;
    }

    private static Dictionary<string, string> ParsearArgumentos(string[] tokens, int numLinha)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < tokens.Length; i++)
        {
            int eq = tokens[i].IndexOf('=');
            if (eq > 0)
                d[tokens[i].Substring(0, eq)] = tokens[i].Substring(eq + 1);
        }
        return d;
    }

    private static DeclaracaoDispositivo LerDispositivo(string[] tokens, Dictionary<string, string> args, int numLinha)
    {
        if (tokens.Length < 3)
            throw new ExcecaoCarga($"Linha {numLinha}: 'disp' requer nome e tipo (bloco|caractere).");
        string nome = tokens[1];
        string tipoStr = tokens[2].ToLowerInvariant();
        TipoDispositivo tipo = tipoStr switch
        {
            "bloco" => TipoDispositivo.Bloco,
            "caractere" => TipoDispositivo.Caractere,
            _ => throw new ExcecaoCarga($"Linha {numLinha}: tipo de dispositivo invalido '{tipoStr}'.")
        };
        long servico = LongOpc(args, "servico", 0, numLinha);
        return new DeclaracaoDispositivo(nome, tipo, servico);
    }

    private static OperacaoArquivo LerOperacaoArquivo(string[] tokens, Dictionary<string, string> args, int numLinha)
    {
        if (tokens.Length < 2)
            throw new ExcecaoCarga($"Linha {numLinha}: 'fs' requer uma suboperacao.");
        string sub = tokens[1].ToLowerInvariant();
        switch (sub)
        {
            case "criararq":
                return new OperacaoArquivo(TipoOperacaoArquivo.CriarArquivo, TextoObrig(args, "caminho", numLinha));
            case "criardir":
                return new OperacaoArquivo(TipoOperacaoArquivo.CriarDiretorio, TextoObrig(args, "caminho", numLinha));
            case "abrir":
            {
                bool escrita = args.TryGetValue("modo", out var m) &&
                               (m.Equals("rw", StringComparison.OrdinalIgnoreCase) ||
                                m.Equals("w", StringComparison.OrdinalIgnoreCase));
                return new OperacaoArquivo(TipoOperacaoArquivo.Abrir,
                    TextoObrig(args, "caminho", numLinha), TextoObrig(args, "handle", numLinha), modoEscrita: escrita);
            }
            case "ler":
                return new OperacaoArquivo(TipoOperacaoArquivo.Ler, handle: TextoObrig(args, "handle", numLinha),
                    tamanho: LongOpc(args, "tam", 0, numLinha));
            case "escrever":
                return new OperacaoArquivo(TipoOperacaoArquivo.Escrever, handle: TextoObrig(args, "handle", numLinha),
                    tamanho: LongOpc(args, "tam", 0, numLinha));
            case "fechar":
                return new OperacaoArquivo(TipoOperacaoArquivo.Fechar, handle: TextoObrig(args, "handle", numLinha));
            case "remover":
                return new OperacaoArquivo(TipoOperacaoArquivo.Remover, TextoObrig(args, "caminho", numLinha));
            case "listar":
                return new OperacaoArquivo(TipoOperacaoArquivo.Listar, TextoObrig(args, "caminho", numLinha));
            default:
                throw new ExcecaoCarga($"Linha {numLinha}: suboperacao de arquivo invalida '{sub}'.");
        }
    }

    private static List<long> LerReferencias(Dictionary<string, string> args, int numLinha)
    {
        var lista = new List<long>();
        if (args.TryGetValue("refs", out var r) && r.Length > 0)
        {
            foreach (var parte in r.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!long.TryParse(parte, out var v) || v < 0)
                    throw new ExcecaoCarga($"Linha {numLinha}: referencia de memoria invalida '{parte}'.");
                lista.Add(v);
            }
        }
        return lista;
    }

    private static bool LerTipoBloqueio(Dictionary<string, string> args, int numLinha)
    {
        if (!args.TryGetValue("tipo", out var t)) return true; // padrao: bloqueante
        return t.ToLowerInvariant() switch
        {
            "bloqueante" => true,
            "naobloqueante" => false,
            _ => throw new ExcecaoCarga($"Linha {numLinha}: tipo de E/S invalido '{t}'.")
        };
    }

    private static int InferirId(string token, ref int seq)
    {
        var digitos = new string(token.Where(char.IsDigit).ToArray());
        if (digitos.Length > 0 && int.TryParse(digitos, out var id)) return id;
        return ++seq;
    }

    private static void ExigirThread(ThreadSimulada? t, int numLinha)
    {
        if (t == null) throw new ExcecaoCarga($"Linha {numLinha}: operacao sem 'thread' anterior.");
    }

    private static string TextoObrig(Dictionary<string, string> args, string chave, int numLinha)
    {
        if (!args.TryGetValue(chave, out var v) || v.Length == 0)
            throw new ExcecaoCarga($"Linha {numLinha}: parametro obrigatorio ausente '{chave}'.");
        return v;
    }

    private static long LongOpc(Dictionary<string, string> args, string chave, long padrao, int numLinha)
    {
        if (!args.TryGetValue(chave, out var v)) return padrao;
        if (!long.TryParse(v, out var r))
            throw new ExcecaoCarga($"Linha {numLinha}: valor numerico invalido em '{chave}={v}'.");
        return r;
    }

    private static int InteiroObrig(string v, string chave, int numLinha)
    {
        if (!int.TryParse(v, out var r))
            throw new ExcecaoCarga($"Linha {numLinha}: valor inteiro invalido em '{chave}={v}'.");
        return r;
    }

    private static void Validar(Carga carga)
    {
        if (carga.Processos.Count == 0)
            throw new ExcecaoCarga("Carga vazia: nenhum processo declarado.");
        var ids = new HashSet<int>();
        foreach (var p in carga.Processos)
        {
            if (!ids.Add(p.Id))
                throw new ExcecaoCarga($"Identificador de processo duplicado: {p.Id}.");
            if (p.Threads.Count == 0)
                throw new ExcecaoCarga($"Processo {p.Id} nao possui threads.");
            foreach (var t in p.Threads)
                if (t.Operacoes.Count == 0)
                    throw new ExcecaoCarga($"Thread {t.Identificacao} nao possui operacoes.");
        }
    }
}
