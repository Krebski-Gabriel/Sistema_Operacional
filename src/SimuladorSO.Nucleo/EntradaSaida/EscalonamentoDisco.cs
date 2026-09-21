using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.EntradaSaida;

/// <summary>Requisição de E/S dirigida a um dispositivo.</summary>
public sealed class RequisicaoES
{
    public ThreadSimulada Thread { get; }
    public long Duracao { get; }
    public bool Bloqueante { get; }
    public long Posicao { get; }
    public long InstanteChegada { get; }

    public RequisicaoES(ThreadSimulada thread, long duracao, bool bloqueante, long posicao, long chegada)
    {
        Thread = thread; Duracao = duracao; Bloqueante = bloqueante; Posicao = posicao; InstanteChegada = chegada;
    }
}

/// <summary>Política de atendimento de requisições de um dispositivo de bloco.</summary>
public interface IEscalonadorDisco
{
    string Nome { get; }
    /// <summary>Escolhe (e remove) a próxima requisição da lista de pendentes.</summary>
    RequisicaoES Selecionar(List<RequisicaoES> pendentes, long posicaoCabecote);
}

/// <summary>FCFS: atende na ordem de chegada.</summary>
public sealed class DiscoFcfs : IEscalonadorDisco
{
    public string Nome => "FCFS";
    public RequisicaoES Selecionar(List<RequisicaoES> pendentes, long posicaoCabecote)
    {
        var r = pendentes[0];
        pendentes.RemoveAt(0);
        return r;
    }
}

/// <summary>SSTF: menor deslocamento em relação à posição atual do cabeçote.</summary>
public sealed class DiscoSstf : IEscalonadorDisco
{
    public string Nome => "SSTF";
    public RequisicaoES Selecionar(List<RequisicaoES> pendentes, long posicaoCabecote)
    {
        int melhor = 0;
        long menorDist = long.MaxValue;
        for (int i = 0; i < pendentes.Count; i++)
        {
            long d = Math.Abs(pendentes[i].Posicao - posicaoCabecote);
            if (d < menorDist) { menorDist = d; melhor = i; }
        }
        var r = pendentes[melhor];
        pendentes.RemoveAt(melhor);
        return r;
    }
}

/// <summary>SCAN (elevador): varre em uma direção até o fim, depois inverte.</summary>
public sealed class DiscoScan : IEscalonadorDisco
{
    private int _direcao = 1; // 1 = crescente, -1 = decrescente
    public string Nome => "SCAN";

    public RequisicaoES Selecionar(List<RequisicaoES> pendentes, long posicaoCabecote)
    {
        // Candidatos na direção atual.
        var adiante = pendentes.Where(r =>
            _direcao == 1 ? r.Posicao >= posicaoCabecote : r.Posicao <= posicaoCabecote).ToList();
        if (adiante.Count == 0)
        {
            _direcao = -_direcao;
            adiante = pendentes.Where(r =>
                _direcao == 1 ? r.Posicao >= posicaoCabecote : r.Posicao <= posicaoCabecote).ToList();
        }
        RequisicaoES escolhida = adiante
            .OrderBy(r => Math.Abs(r.Posicao - posicaoCabecote))
            .First();
        pendentes.Remove(escolhida);
        return escolhida;
    }
}
