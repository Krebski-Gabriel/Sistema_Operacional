using SimuladorSO.Nucleo.Arquivos;
using SimuladorSO.Nucleo.Comum;
using SimuladorSO.Nucleo.EntradaSaida;
using SimuladorSO.Nucleo.Escalonamento;
using SimuladorSO.Nucleo.Log;
using SimuladorSO.Nucleo.Memoria;
using SimuladorSO.Nucleo.Processos;
using SimuladorSO.Nucleo.Metricas;

namespace SimuladorSO.Nucleo.Nucleo;

/// <summary>
/// Monta um <see cref="NucleoSimulacao"/> a partir da configuração e da carga,
/// selecionando as políticas substituíveis conforme a configuração.
/// </summary>
public static class FabricaSimulador
{
    public static NucleoSimulacao Construir(Configuracao config, Carga.Carga carga, RegistradorEventos log)
    {
        IEscalonador escalonador = config.Escalonamento switch
        {
            PoliticaEscalonamento.Fcfs => new EscalonadorFcfs(),
            PoliticaEscalonamento.RoundRobin => new EscalonadorRoundRobin(),
            PoliticaEscalonamento.Prioridade => new EscalonadorPrioridade(
                config.PrioridadePreemptiva, config.EnvelhecimentoAtivo, config.IntervaloEnvelhecimento),
            _ => throw new ArgumentOutOfRangeException()
        };

        IPoliticaSubstituicao politicaSub = config.Substituicao switch
        {
            PoliticaSubstituicao.Fifo => new SubstituicaoFifo(),
            PoliticaSubstituicao.Lru => new SubstituicaoLru(),
            _ => throw new ArgumentOutOfRangeException()
        };

        var memoria = new GerenciadorMemoria(config.TamanhoPagina, config.NumeroMolduras, politicaSub);
        var arquivos = new SistemaDeArquivos(config.TotalBlocosDisco, config.TamanhoBloco);
        var dispositivos = ConstruirDispositivos(config, carga);
        var metricas = new ColetorMetricas();

        return new NucleoSimulacao(config, escalonador, memoria, arquivos, dispositivos, metricas, log, carga.Processos);
    }

    private static IReadOnlyDictionary<string, Dispositivo> ConstruirDispositivos(Configuracao config, Carga.Carga carga)
    {
        IEscalonadorDisco NovaPoliticaDisco() => config.EscalonamentoDisco switch
        {
            PoliticaDisco.Fcfs => new DiscoFcfs(),
            PoliticaDisco.Sstf => new DiscoSstf(),
            PoliticaDisco.Scan => new DiscoScan(),
            _ => new DiscoFcfs()
        };

        var mapa = new Dictionary<string, Dispositivo>(StringComparer.Ordinal);

        // Dispositivos declarados pela carga.
        foreach (var d in carga.Dispositivos)
        {
            long servico = d.TempoServico > 0 ? d.TempoServico : config.TempoServicoDispositivoPadrao;
            var pol = d.Tipo == TipoDispositivo.Bloco ? NovaPoliticaDisco() : (IEscalonadorDisco)new DiscoFcfs();
            mapa[d.Nome] = new Dispositivo(d.Nome, d.Tipo, servico, pol);
        }

        // Garante ao menos um dispositivo de bloco e um de caractere.
        if (!mapa.Values.Any(v => v.Tipo == TipoDispositivo.Bloco))
            mapa["disco"] = new Dispositivo("disco", TipoDispositivo.Bloco,
                config.TempoServicoDispositivoPadrao, NovaPoliticaDisco());
        if (!mapa.Values.Any(v => v.Tipo == TipoDispositivo.Caractere))
            mapa["console"] = new Dispositivo("console", TipoDispositivo.Caractere,
                config.TempoServicoDispositivoPadrao, new DiscoFcfs());

        // Cria automaticamente qualquer dispositivo referenciado e ainda inexistente.
        foreach (var p in carga.Processos)
            foreach (var t in p.Threads)
                foreach (var op in t.Operacoes)
                    if (op is OperacaoES io && !mapa.ContainsKey(io.Dispositivo))
                    {
                        bool ehBloco = ParecerBloco(io.Dispositivo);
                        var tipo = ehBloco ? TipoDispositivo.Bloco : TipoDispositivo.Caractere;
                        var pol = ehBloco ? NovaPoliticaDisco() : (IEscalonadorDisco)new DiscoFcfs();
                        mapa[io.Dispositivo] = new Dispositivo(io.Dispositivo, tipo,
                            config.TempoServicoDispositivoPadrao, pol);
                    }

        return mapa;
    }

    private static bool ParecerBloco(string nome)
    {
        string n = nome.ToLowerInvariant();
        return n.Contains("disc") || n.Contains("disk") || n.Contains("hd") || n.Contains("ssd") || n.Contains("bloco");
    }
}
