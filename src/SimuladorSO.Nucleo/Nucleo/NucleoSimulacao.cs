using SimuladorSO.Nucleo.Arquivos;
using SimuladorSO.Nucleo.Comum;
using SimuladorSO.Nucleo.EntradaSaida;
using SimuladorSO.Nucleo.Escalonamento;
using SimuladorSO.Nucleo.Log;
using SimuladorSO.Nucleo.Memoria;
using SimuladorSO.Nucleo.Metricas;
using SimuladorSO.Nucleo.Processos;

namespace SimuladorSO.Nucleo.Nucleo;

/// <summary>
/// Núcleo da simulação orientada a eventos discretos. Coordena o clock lógico e a
/// fila de eventos, e conecta os módulos (escalonador, memória, E/S, arquivos)
/// por interfaces bem definidas. A execução da CPU avança em unidades de tempo por
/// meio de eventos, o que garante interação correta com carregamentos de página
/// concorrentes e permite verificar quantum e preempção em fronteiras de unidade.
/// </summary>
public sealed class NucleoSimulacao
{
    private readonly Configuracao _config;
    private readonly IEscalonador _escalonador;
    private readonly GerenciadorMemoria _memoria;
    private readonly SistemaDeArquivos _arquivos;
    private readonly IReadOnlyDictionary<string, Dispositivo> _dispositivos;
    private readonly ColetorMetricas _metricas;
    private readonly RegistradorEventos _log;
    private readonly FilaDeEventos _fila = new();
    private readonly IReadOnlyList<Processo> _processos;

    private long _clock;
    private ThreadSimulada? _threadNaCpu;
    private bool _trocaPendente;

    public long Clock => _clock;
    public ColetorMetricas Metricas => _metricas;
    public IReadOnlyDictionary<string, Dispositivo> Dispositivos => _dispositivos;
    public SistemaDeArquivos Arquivos => _arquivos;

    public NucleoSimulacao(
        Configuracao config,
        IEscalonador escalonador,
        GerenciadorMemoria memoria,
        SistemaDeArquivos arquivos,
        IReadOnlyDictionary<string, Dispositivo> dispositivos,
        ColetorMetricas metricas,
        RegistradorEventos log,
        IReadOnlyList<Processo> processos)
    {
        _config = config;
        _escalonador = escalonador;
        _memoria = memoria;
        _arquivos = arquivos;
        _dispositivos = dispositivos;
        _metricas = metricas;
        _log = log;
        _processos = processos;
    }

    // ================= Laço principal =================

    public ColetorMetricas Executar()
    {
        _log.RegistrarConfiguracao(_config.Descricao());

        foreach (var p in _processos)
        {
            _metricas.RegistrarProcesso(p);
            foreach (var t in p.Threads) _metricas.RegistrarThread(t);
            _fila.Agendar(new EventoChegada(p.Chegada, p));
        }

        while (!_fila.Vazia)
        {
            var evento = _fila.Desenfileirar();
            _clock = evento.Tempo;
            evento.Executar(this);
        }

        _metricas.DefinirTempoFinal(_clock);
        _log.Registrar(_clock, TipoEventoLog.Encerramento, "-", "Simulacao encerrada.");
        return _metricas;
    }

    private void Agendar(long tempo, IEvento evento) => _fila.Agendar(evento);

    // ================= Chegada =================

    public void ProcessarChegada(Processo processo)
    {
        processo.Pcb.Estado = EstadoProcesso.Ativo;
        foreach (var t in processo.Threads)
        {
            _log.Registrar(_clock, TipoEventoLog.Chegada, t.Identificacao,
                $"Thread chega (prioridade={t.PrioridadeBase}).");
            t.Estado = Comum.EstadoThread.Pronta;
            _escalonador.Admitir(t, _clock);
            _metricas.EntrarPronto(t, _clock);
            _log.Registrar(_clock, TipoEventoLog.Admissao, t.Identificacao, "Admitida na fila de prontos.");
        }
        TentarDespachar();
    }

    // ================= Escalonamento / CPU =================

    private void TentarDespachar()
    {
        if (_threadNaCpu != null || _trocaPendente || !_escalonador.TemProntas) return;

        var prox = _escalonador.Selecionar(_clock);
        _trocaPendente = true;
        _metricas.RegistrarTrocaContexto(_config.CustoTrocaContexto);
        if (_config.CustoTrocaContexto > 0)
            _log.Registrar(_clock, TipoEventoLog.InicioTrocaContexto, prox.Identificacao,
                $"Troca de contexto ({_config.CustoTrocaContexto} u.t.).");
        Agendar(_clock + _config.CustoTrocaContexto, new EventoDespacho(_clock + _config.CustoTrocaContexto, prox));
    }

    public void ProcessarDespacho(ThreadSimulada t)
    {
        _trocaPendente = false;
        if (_config.CustoTrocaContexto > 0)
            _log.Registrar(_clock, TipoEventoLog.FimTrocaContexto, t.Identificacao, "Contexto carregado.");

        _threadNaCpu = t;
        t.Estado = Comum.EstadoThread.Executando;
        t.QuantumConsumido = 0;
        _metricas.SairPronto(t, _clock);
        _metricas.RegistrarResposta(t, _clock);
        _log.Registrar(_clock, TipoEventoLog.Despacho, t.Identificacao, "Recebe a CPU.");
        Agendar(_clock, new EventoExecucaoCpu(_clock, t));
    }

    public void ProcessarExecucaoCpu(ThreadSimulada t)
    {
        if (!ReferenceEquals(_threadNaCpu, t) || t.Estado != Comum.EstadoThread.Executando)
            return; // evento obsoleto

        var op = t.OperacaoAtual;
        if (op == null) { FinalizarThread(t); return; }

        switch (op)
        {
            case SurtoCpu surto:
                ExecutarPassoCpu(t, surto);
                break;
            case OperacaoES io:
                ProcessarOperacaoES(t, io);
                break;
            case OperacaoArquivo fa:
                ExecutarOperacaoArquivo(t, fa);
                AvancarIndice(t);
                Agendar(_clock, new EventoExecucaoCpu(_clock, t)); // continua na CPU
                break;
        }
    }

    private void ExecutarPassoCpu(ThreadSimulada t, SurtoCpu surto)
    {
        // Fim do surto?
        if (t.PassosExecutadosNoSurto >= surto.Passos.Count)
        {
            _log.Registrar(_clock, TipoEventoLog.FimBurst, t.Identificacao, "Surto de CPU concluido.");
            AvancarIndice(t);
            if (t.OperacaoAtual == null) { FinalizarThread(t); return; }
            Agendar(_clock, new EventoExecucaoCpu(_clock, t));
            return;
        }

        // Fim de quantum (Round Robin)?
        if (_escalonador.UsaQuantum && t.QuantumConsumido >= _config.Quantum)
        {
            Preemptar(t, "fim de quantum");
            return;
        }

        // Preempcao por prioridade?
        if (_escalonador.Preemptivo && _escalonador.DevePreemptar(t, _clock))
        {
            Preemptar(t, "prioridade superior pronta");
            return;
        }

        // Executa um passo
        long? passo = surto.Passos[t.PassosExecutadosNoSurto];
        if (passo.HasValue)
        {
            long endereco = passo.Value;
            if (t.RetomandoAposFalta)
            {
                t.RetomandoAposFalta = false; // retomada do passo que faltou (pagina agora presente)
            }
            else
            {
                _metricas.RegistrarReferencia();
                var res = _memoria.Acessar(t.Processo.Pcb.EspacoEnderecamento, endereco);
                if (res == ResultadoAcesso.Falta)
                {
                    _metricas.RegistrarFalta();
                    long pagina = _memoria.PaginaDe(endereco);
                    _log.Registrar(_clock, TipoEventoLog.FaltaPagina, t.Identificacao,
                        $"Falta na pagina {pagina} (end={endereco}).");
                    t.Estado = Comum.EstadoThread.Bloqueada;
                    t.RetomandoAposFalta = true;
                    _log.Registrar(_clock, TipoEventoLog.Bloqueio, t.Identificacao, "Bloqueada aguardando pagina.");
                    _threadNaCpu = null;
                    Agendar(_clock + _config.TempoAtendimentoFalta,
                        new EventoFimAtendimentoFalta(_clock + _config.TempoAtendimentoFalta, t, endereco));
                    TentarDespachar();
                    return;
                }
                _metricas.RegistrarAcerto();
            }
        }

        // Consome uma unidade de CPU
        _metricas.RegistrarUsoCpu(1);
        t.PassosExecutadosNoSurto++;
        t.QuantumConsumido++;
        t.Tcb.ContadorProgramaLogico++;
        Agendar(_clock + 1, new EventoExecucaoCpu(_clock + 1, t));
    }

    private void Preemptar(ThreadSimulada t, string motivo)
    {
        _log.Registrar(_clock, TipoEventoLog.Preempcao, t.Identificacao, $"Preemptada ({motivo}).");
        t.Estado = Comum.EstadoThread.Pronta;
        _threadNaCpu = null;
        _escalonador.Admitir(t, _clock);
        _metricas.EntrarPronto(t, _clock);
        TentarDespachar();
    }

    private void ProcessarOperacaoES(ThreadSimulada t, OperacaoES io)
    {
        if (!_dispositivos.TryGetValue(io.Dispositivo, out var dispositivo))
        {
            _log.Registrar(_clock, TipoEventoLog.ErroOperacao, t.Identificacao,
                $"Dispositivo inexistente: {io.Dispositivo}.");
            AvancarIndice(t);
            Agendar(_clock, new EventoExecucaoCpu(_clock, t));
            return;
        }

        var req = new RequisicaoES(t, io.Duracao, io.Bloqueante, io.Posicao, _clock);

        if (io.Bloqueante)
        {
            _log.Registrar(_clock, TipoEventoLog.SolicitacaoES, t.Identificacao,
                $"Solicita E/S bloqueante em {io.Dispositivo}.");
            t.Estado = Comum.EstadoThread.Bloqueada;
            _log.Registrar(_clock, TipoEventoLog.Bloqueio, t.Identificacao, "Bloqueada aguardando E/S.");
            _threadNaCpu = null;
            AvancarIndice(t); // ao desbloquear, retoma na operacao seguinte
            dispositivo.Enfileirar(req);
            TentarIniciarDispositivo(dispositivo);
            TentarDespachar();
        }
        else
        {
            _log.Registrar(_clock, TipoEventoLog.SolicitacaoES, t.Identificacao,
                $"Solicita E/S nao bloqueante em {io.Dispositivo}.");
            dispositivo.Enfileirar(req);
            TentarIniciarDispositivo(dispositivo);
            AvancarIndice(t); // prossegue sem bloquear (proibida espera ativa)
            Agendar(_clock, new EventoExecucaoCpu(_clock, t));
        }
    }

    // ================= Entrada e saída =================

    private void TentarIniciarDispositivo(Dispositivo dispositivo)
    {
        if (dispositivo.Ocupado || !dispositivo.TemPendentes) return;
        var req = dispositivo.IniciarProxima();
        long servico = dispositivo.TempoServicoDe(req);
        Agendar(_clock + servico, new EventoConclusaoES(_clock + servico, dispositivo, req));
    }

    public void ProcessarConclusaoES(Dispositivo dispositivo, RequisicaoES req)
    {
        dispositivo.Concluir();
        var t = req.Thread;
        _log.Registrar(_clock, TipoEventoLog.ConclusaoES, t.Identificacao,
            $"Conclusao de E/S em {dispositivo.Nome} (interrupcao).");

        if (req.Bloqueante)
        {
            _log.Registrar(_clock, TipoEventoLog.Interrupcao, t.Identificacao, "Interrupcao: thread desbloqueada.");
            t.Estado = Comum.EstadoThread.Pronta;
            _escalonador.Admitir(t, _clock);
            _metricas.EntrarPronto(t, _clock);
            TentarDespachar();
        }
        else
        {
            _log.Registrar(_clock, TipoEventoLog.Interrupcao, t.Identificacao,
                "Interrupcao: resultado de E/S nao bloqueante entregue.");
        }

        TentarIniciarDispositivo(dispositivo);
    }

    // ================= Memória =================

    public void ProcessarFimAtendimentoFalta(ThreadSimulada t, long endereco)
    {
        var carga = _memoria.TratarFalta(t.Processo.Id, t.Processo.Pcb.EspacoEnderecamento, endereco);
        _metricas.RegistrarCarregamento();
        long pagina = _memoria.PaginaDe(endereco);
        _log.Registrar(_clock, TipoEventoLog.CarregamentoPagina, t.Identificacao,
            $"Pagina {pagina} carregada na moldura {carga.Moldura}.");
        if (carga.HouveSubstituicao)
        {
            _metricas.RegistrarSubstituicao();
            _log.Registrar(_clock, TipoEventoLog.SubstituicaoPagina, t.Identificacao,
                $"Vitima removida: proc {carga.ProcessoVitima} pag {carga.PaginaVitima} (moldura {carga.Moldura}).");
        }

        t.Estado = Comum.EstadoThread.Pronta;
        _escalonador.Admitir(t, _clock);
        _metricas.EntrarPronto(t, _clock);
        TentarDespachar();
    }

    // ================= Arquivos =================

    private void ExecutarOperacaoArquivo(ThreadSimulada t, OperacaoArquivo fa)
    {
        var descritores = t.Processo.Pcb.ArquivosAbertos;
        try
        {
            switch (fa.Tipo)
            {
                case TipoOperacaoArquivo.CriarArquivo:
                    _arquivos.CriarArquivo(fa.Caminho, _clock);
                    RegistrarFs(t, $"Arquivo criado: {fa.Caminho}."); break;
                case TipoOperacaoArquivo.CriarDiretorio:
                    _arquivos.CriarDiretorio(fa.Caminho, _clock);
                    RegistrarFs(t, $"Diretorio criado: {fa.Caminho}."); break;
                case TipoOperacaoArquivo.Abrir:
                    _arquivos.Abrir(descritores, fa.Caminho, fa.Handle, fa.ModoEscrita, _clock);
                    RegistrarFs(t, $"Aberto {fa.Caminho} como '{fa.Handle}' ({(fa.ModoEscrita ? "rw" : "r")})."); break;
                case TipoOperacaoArquivo.Ler:
                    long lido = _arquivos.Ler(descritores, fa.Handle, fa.Tamanho);
                    RegistrarFs(t, $"Leitura de {lido} bytes via '{fa.Handle}'."); break;
                case TipoOperacaoArquivo.Escrever:
                    _arquivos.Escrever(descritores, fa.Handle, fa.Tamanho, _clock);
                    RegistrarFs(t, $"Escrita de {fa.Tamanho} bytes via '{fa.Handle}'."); break;
                case TipoOperacaoArquivo.Fechar:
                    _arquivos.Fechar(descritores, fa.Handle);
                    RegistrarFs(t, $"Fechado descritor '{fa.Handle}'."); break;
                case TipoOperacaoArquivo.Remover:
                    _arquivos.Remover(fa.Caminho, _clock);
                    RegistrarFs(t, $"Removido: {fa.Caminho}."); break;
                case TipoOperacaoArquivo.Listar:
                    var itens = _arquivos.Listar(fa.Caminho);
                    RegistrarFs(t, $"Listagem de {fa.Caminho}: [{string.Join(", ", itens.Select(i => i.Nome))}]."); break;
            }
        }
        catch (ExcecaoSistemaArquivos ex)
        {
            _log.Registrar(_clock, TipoEventoLog.ErroOperacao, t.Identificacao,
                $"Operacao invalida ({fa.Tipo}): {ex.Message}");
        }
    }

    private void RegistrarFs(ThreadSimulada t, string descricao)
        => _log.Registrar(_clock, TipoEventoLog.OperacaoArquivo, t.Identificacao, descricao);

    // ================= Finalização =================

    private void FinalizarThread(ThreadSimulada t)
    {
        t.Estado = Comum.EstadoThread.Finalizada;
        _log.Registrar(_clock, TipoEventoLog.FimThread, t.Identificacao, "Thread finalizada.");
        _metricas.FinalizarThread(t, _clock);
        if (ReferenceEquals(_threadNaCpu, t)) _threadNaCpu = null;

        var processo = t.Processo;
        if (processo.TodasThreadsFinalizadas() && processo.Pcb.Estado != EstadoProcesso.Finalizado)
        {
            _arquivos.FecharTodos(processo.Pcb.ArquivosAbertos);
            _memoria.LiberarProcesso(processo.Id);
            processo.Pcb.Estado = EstadoProcesso.Finalizado;
            _log.Registrar(_clock, TipoEventoLog.FimProcesso, $"P{processo.Id}",
                "Processo finalizado; recursos liberados.");
            _metricas.FinalizarProcesso(processo, _clock);
        }
        TentarDespachar();
    }

    private static void AvancarIndice(ThreadSimulada t)
    {
        t.IndiceOperacao++;
        t.PassosExecutadosNoSurto = 0;
    }
}
