namespace SimuladorSO.Nucleo.Memoria;

/// <summary>
/// Política de substituição de páginas (mecanismo x política separados).
/// O gerenciador de memória usa esta interface sem conhecer o algoritmo concreto.
/// </summary>
public interface IPoliticaSubstituicao
{
    string Nome { get; }
    /// <summary>Registra que uma moldura recebeu uma nova página (carregamento).</summary>
    void AoCarregar(int moldura);
    /// <summary>Registra um acesso (acerto) a uma moldura já residente.</summary>
    void AoAcessar(int moldura);
    /// <summary>Escolhe a moldura vítima quando não há molduras livres.</summary>
    int EscolherVitima();
    /// <summary>Remove a moldura das estruturas de controle (quando liberada).</summary>
    void AoLiberar(int moldura);
}

/// <summary>Substituição FIFO: a página mais antiga em memória é a vítima.</summary>
public sealed class SubstituicaoFifo : IPoliticaSubstituicao
{
    private readonly Queue<int> _ordem = new();
    public string Nome => "FIFO";

    public void AoCarregar(int moldura) => _ordem.Enqueue(moldura);
    public void AoAcessar(int moldura) { /* FIFO não considera acessos */ }

    public int EscolherVitima()
    {
        if (_ordem.Count == 0)
            throw new InvalidOperationException("FIFO: nenhuma moldura para substituir.");
        return _ordem.Dequeue();
    }

    public void AoLiberar(int moldura)
    {
        // Reconstrói a fila sem a moldura liberada (operação rara).
        var restantes = _ordem.Where(m => m != moldura).ToArray();
        _ordem.Clear();
        foreach (var m in restantes) _ordem.Enqueue(m);
    }
}

/// <summary>
/// Substituição LRU (extensão): a página menos recentemente usada é a vítima.
/// Usa um contador lógico de recência mantido por moldura.
/// </summary>
public sealed class SubstituicaoLru : IPoliticaSubstituicao
{
    private readonly Dictionary<int, long> _recencia = new();
    private long _relogio;
    public string Nome => "LRU";

    public void AoCarregar(int moldura) => _recencia[moldura] = ++_relogio;
    public void AoAcessar(int moldura) => _recencia[moldura] = ++_relogio;

    public int EscolherVitima()
    {
        if (_recencia.Count == 0)
            throw new InvalidOperationException("LRU: nenhuma moldura para substituir.");
        int vitima = -1;
        long menor = long.MaxValue;
        foreach (var kv in _recencia)
            if (kv.Value < menor) { menor = kv.Value; vitima = kv.Key; }
        _recencia.Remove(vitima);
        return vitima;
    }

    public void AoLiberar(int moldura) => _recencia.Remove(moldura);
}
