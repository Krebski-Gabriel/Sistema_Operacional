namespace SimuladorSO.Nucleo.Log;

/// <summary>
/// Tipos de evento relevantes registrados no log. Cada linha do log permite
/// reconstruir o que ocorreu na simulação, em ordem temporal.
/// </summary>
public enum TipoEventoLog
{
    Configuracao,
    Chegada,
    Admissao,
    InicioTrocaContexto,
    FimTrocaContexto,
    Despacho,
    Preempcao,
    Bloqueio,
    Interrupcao,
    SolicitacaoES,
    ConclusaoES,
    AcessoMemoria,
    FaltaPagina,
    CarregamentoPagina,
    SubstituicaoPagina,
    OperacaoArquivo,
    ErroOperacao,
    FimBurst,
    FimThread,
    FimProcesso,
    Ociosa,
    Encerramento
}

/// <summary>
/// Registra os eventos da simulação em um <see cref="TextWriter"/> (arquivo ou
/// console). Cada linha contém: clock, tipo do evento, objetos envolvidos e uma
/// descrição concisa. É um mecanismo puro: não decide nada sobre a simulação.
/// </summary>
public sealed class RegistradorEventos
{
    private readonly TextWriter _saida;
    private readonly bool _habilitado;
    public int TotalLinhas { get; private set; }

    public RegistradorEventos(TextWriter saida, bool habilitado = true)
    {
        _saida = saida;
        _habilitado = habilitado;
    }

    public void RegistrarConfiguracao(string descricaoConfig)
    {
        if (!_habilitado) return;
        _saida.WriteLine(descricaoConfig);
        _saida.WriteLine($"{"CLOCK",8} | {"EVENTO",-18} | {"OBJETOS",-22} | DESCRICAO");
        _saida.WriteLine(new string('-', 90));
    }

    public void Registrar(long clock, TipoEventoLog tipo, string objetos, string descricao)
    {
        if (!_habilitado) return;
        TotalLinhas++;
        _saida.WriteLine($"{clock,8} | {tipo,-18} | {objetos,-22} | {descricao}");
    }

    public void LinhaLivre(string texto)
    {
        if (!_habilitado) return;
        _saida.WriteLine(texto);
    }
}
