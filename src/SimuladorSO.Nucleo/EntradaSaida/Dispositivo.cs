namespace SimuladorSO.Nucleo.EntradaSaida;

public enum TipoDispositivo { Bloco, Caractere }

/// <summary>
/// Dispositivo de E/S. Mantém a própria fila de requisições, o estado (ocioso ou
/// ocupado), a posição do cabeçote (para dispositivos de bloco) e um tempo de
/// serviço padrão. O agendamento dos eventos de conclusão é conduzido pelo núcleo;
/// aqui ficam a fila e a política de atendimento.
/// </summary>
public sealed class Dispositivo
{
    private readonly List<RequisicaoES> _pendentes = new();
    private readonly IEscalonadorDisco _politica;

    public string Nome { get; }
    public TipoDispositivo Tipo { get; }
    public long TempoServicoPadrao { get; }
    public bool Ocupado { get; private set; }
    public long PosicaoCabecote { get; private set; }
    public RequisicaoES? EmAtendimento { get; private set; }

    // Métricas do dispositivo
    public long TempoOcupado { get; private set; }
    public long RequisicoesAtendidas { get; private set; }
    public long DeslocamentoTotal { get; private set; }

    public Dispositivo(string nome, TipoDispositivo tipo, long tempoServicoPadrao, IEscalonadorDisco politica)
    {
        Nome = nome; Tipo = tipo; TempoServicoPadrao = tempoServicoPadrao; _politica = politica;
    }

    public bool TemPendentes => _pendentes.Count > 0;
    public int QuantidadePendentes => _pendentes.Count;

    public void Enfileirar(RequisicaoES req) => _pendentes.Add(req);

    /// <summary>Inicia o atendimento da próxima requisição. Devolve a requisição escolhida
    /// e registra a ocupação e o deslocamento do cabeçote.</summary>
    public RequisicaoES IniciarProxima()
    {
        if (Ocupado) throw new InvalidOperationException($"Dispositivo {Nome} ja ocupado.");
        if (_pendentes.Count == 0) throw new InvalidOperationException($"Dispositivo {Nome} sem requisicoes.");

        var req = Tipo == TipoDispositivo.Bloco
            ? _politica.Selecionar(_pendentes, PosicaoCabecote)
            : SelecionarFcfs();

        if (Tipo == TipoDispositivo.Bloco)
        {
            DeslocamentoTotal += Math.Abs(req.Posicao - PosicaoCabecote);
            PosicaoCabecote = req.Posicao;
        }

        Ocupado = true;
        EmAtendimento = req;
        long servico = TempoServicoDe(req);
        TempoOcupado += servico;
        return req;
    }

    public long TempoServicoDe(RequisicaoES req) => req.Duracao > 0 ? req.Duracao : TempoServicoPadrao;

    public void Concluir()
    {
        Ocupado = false;
        EmAtendimento = null;
        RequisicoesAtendidas++;
    }

    public string PoliticaNome => Tipo == TipoDispositivo.Bloco ? _politica.Nome : "FCFS";

    private RequisicaoES SelecionarFcfs()
    {
        var r = _pendentes[0];
        _pendentes.RemoveAt(0);
        return r;
    }
}
