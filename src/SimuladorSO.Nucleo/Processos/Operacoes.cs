namespace SimuladorSO.Nucleo.Processos;

/// <summary>
/// Uma operação da sequência de execução de uma thread. As threads executam
/// operações em ordem: surtos de CPU, operações de E/S e operações de arquivo.
/// </summary>
public interface IOperacao
{
    string Resumo();
}

/// <summary>
/// Surto de CPU. É composto por "passos"; cada passo consome uma unidade de
/// tempo lógico de CPU. Um passo pode ser puro processamento (Referencia == null)
/// ou um acesso à memória em um endereço lógico (Referencia != null), que pode
/// gerar acerto ou falta de página.
/// </summary>
public sealed class SurtoCpu : IOperacao
{
    /// <summary>Cada elemento é um endereço lógico referenciado, ou null para processamento puro.</summary>
    public IReadOnlyList<long?> Passos { get; }

    public SurtoCpu(IReadOnlyList<long?> passos)
    {
        if (passos.Count == 0)
            throw new ArgumentException("Um surto de CPU deve ter ao menos um passo (duracao > 0).");
        Passos = passos;
    }

    /// <summary>Constrói um surto de duração fixa e uma lista opcional de endereços referenciados.</summary>
    public static SurtoCpu Criar(long duracao, IReadOnlyList<long>? referencias = null)
    {
        var passos = new List<long?>();
        int nRef = referencias?.Count ?? 0;
        long total = Math.Max(duracao, nRef);
        for (int i = 0; i < total; i++)
            passos.Add(i < nRef ? referencias![i] : (long?)null);
        return new SurtoCpu(passos);
    }

    public long Duracao => Passos.Count;

    public string Resumo() => $"cpu[dur={Duracao},refs={Passos.Count(p => p.HasValue)}]";
}

/// <summary>
/// Operação de entrada e saída dirigida a um dispositivo. Pode ser bloqueante
/// (a thread deixa a CPU até a conclusão) ou não bloqueante (a thread prossegue).
/// </summary>
public sealed class OperacaoES : IOperacao
{
    public string Dispositivo { get; }
    public long Duracao { get; }
    public bool Bloqueante { get; }
    /// <summary>Posição/cilindro alvo (usado apenas por escalonamento de disco SSTF/SCAN).</summary>
    public long Posicao { get; }

    public OperacaoES(string dispositivo, long duracao, bool bloqueante, long posicao = 0)
    {
        Dispositivo = dispositivo;
        Duracao = duracao;
        Bloqueante = bloqueante;
        Posicao = posicao;
    }

    public string Resumo() =>
        $"io[{Dispositivo},dur={Duracao},{(Bloqueante ? "bloq" : "naobloq")},pos={Posicao}]";
}

public enum TipoOperacaoArquivo
{
    CriarArquivo, CriarDiretorio, Abrir, Ler, Escrever, Fechar, Remover, Listar
}

/// <summary>
/// Operação sobre o sistema de arquivos executada por uma thread. As operações
/// de arquivo são resolvidas em tempo lógico nulo (metadados), mas geram eventos
/// e podem falhar (operação inválida), o que é detectado e registrado.
/// </summary>
public sealed class OperacaoArquivo : IOperacao
{
    public TipoOperacaoArquivo Tipo { get; }
    public string Caminho { get; }
    /// <summary>Nome simbólico do descritor aberto pela thread (para abrir/ler/escrever/fechar).</summary>
    public string Handle { get; }
    public long Tamanho { get; }
    public bool ModoEscrita { get; }

    public OperacaoArquivo(TipoOperacaoArquivo tipo, string caminho = "", string handle = "",
                           long tamanho = 0, bool modoEscrita = false)
    {
        Tipo = tipo;
        Caminho = caminho;
        Handle = handle;
        Tamanho = tamanho;
        ModoEscrita = modoEscrita;
    }

    public string Resumo() => $"fs[{Tipo},{Caminho}{(Handle.Length > 0 ? $",h={Handle}" : "")}]";
}
