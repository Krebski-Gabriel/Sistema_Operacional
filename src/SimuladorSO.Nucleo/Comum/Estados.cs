namespace SimuladorSO.Nucleo.Comum;

/// <summary>
/// Estados do ciclo de vida de uma thread (unidade escalonável).
/// Toda transição entre estados ocorre por um evento identificável e é registrada no log.
/// </summary>
public enum EstadoThread
{
    Nova,
    Pronta,
    Executando,
    Bloqueada,
    Finalizada
}

/// <summary>
/// Estados do ciclo de vida de um processo. Um processo é considerado
/// Finalizado quando todas as suas threads terminam.
/// </summary>
public enum EstadoProcesso
{
    Novo,
    Ativo,
    Finalizado
}
