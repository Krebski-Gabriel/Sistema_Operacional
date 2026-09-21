using SimuladorSO.Nucleo.Arquivos;
using SimuladorSO.Nucleo.Comum;
using SimuladorSO.Nucleo.Memoria;

namespace SimuladorSO.Nucleo.Processos;

/// <summary>
/// Bloco de Controle de Processo (PCB). Concentra as informações necessárias à
/// administração do processo. Cada campo tem função definida no simulador.
/// </summary>
public sealed class Pcb
{
    public int Id { get; }
    public EstadoProcesso Estado { get; set; } = EstadoProcesso.Novo;
    public int Prioridade { get; }
    public long InstanteCriacao { get; }
    public long ContadorProgramaLogico { get; set; }
    public long[] Registradores { get; } = new long[4]; // registradores simulados
    public TabelaDePaginas EspacoEnderecamento { get; } = new();
    public List<ThreadSimulada> Threads { get; } = new();
    public Dictionary<string, DescritorAberto> ArquivosAbertos { get; } = new();

    public Pcb(int id, int prioridade, long instanteCriacao)
    {
        Id = id;
        Prioridade = prioridade;
        InstanteCriacao = instanteCriacao;
    }
}

/// <summary>
/// Bloco de Controle de Thread (TCB). Cada thread pertence a exatamente um
/// processo e mantém seu próprio contexto lógico.
/// </summary>
public sealed class Tcb
{
    public string Id { get; }
    public EstadoThread Estado { get; set; } = EstadoThread.Nova;
    public Processo Processo { get; }
    public long ContadorProgramaLogico { get; set; }
    public long[] Registradores { get; } = new long[4]; // registradores simulados
    public Stack<long> PilhaLogica { get; } = new();     // pilha lógica

    public Tcb(string id, Processo processo)
    {
        Id = id;
        Processo = processo;
    }
}
