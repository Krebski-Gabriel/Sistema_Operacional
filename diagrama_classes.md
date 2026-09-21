# Diagrama de Classes

O diagrama abaixo (notação Mermaid) resume as principais classes do simulador e
suas relações. Ele destaca a **separação mecanismo × política**: o
`NucleoSimulacao` depende apenas de interfaces (`IEscalonador`,
`IEscalonadorDisco`, `IPoliticaSubstituicao`), cujas implementações concretas
são escolhidas pela `FabricaSimulador`.

```mermaid
classDiagram
    direction LR

    class NucleoSimulacao {
        +long Clock
        +ColetorMetricas Executar()
        -TentarDespachar()
        -ProcessarExecucaoCpu()
        -ProcessarOperacaoES()
        -ProcessarFimAtendimentoFalta()
        -ExecutarOperacaoArquivo()
    }
    class FabricaSimulador {
        +Construir(Configuracao, Carga, RegistradorEventos) NucleoSimulacao
    }
    class Configuracao
    class RegistradorEventos
    class ColetorMetricas
    class FilaDeEventos
    class IEvento {
        <<interface>>
        +long Tempo
        +long Sequencia
        +Executar(NucleoSimulacao)
    }

    class IEscalonador {
        <<interface>>
        +Admitir(ThreadSimulada)
        +Selecionar() ThreadSimulada
        +DevePreemptar(ThreadSimulada) bool
    }
    class EscalonadorFcfs
    class EscalonadorRoundRobin
    class EscalonadorPrioridade

    class GerenciadorMemoria {
        +Acessar(TabelaDePaginas, long) ResultadoAcesso
        +TratarFalta(int, TabelaDePaginas, long) ResultadoCarga
        +LiberarProcesso(int)
    }
    class IPoliticaSubstituicao {
        <<interface>>
        +AoCarregar(int)
        +AoAcessar(int)
        +EscolherVitima() int
    }
    class SubstituicaoFifo
    class SubstituicaoLru
    class TabelaDePaginas

    class Dispositivo {
        +Enfileirar(RequisicaoES)
        +IniciarProxima() RequisicaoES
        +Concluir()
    }
    class IEscalonadorDisco {
        <<interface>>
        +Selecionar(List~RequisicaoES~, long) RequisicaoES
    }
    class DiscoFcfs
    class DiscoSstf
    class DiscoScan

    class SistemaDeArquivos {
        +CriarArquivo(caminho, clock)
        +Abrir(descritores, caminho, handle, escrita, clock)
        +Ler(...) int
        +Escrever(...)
        +Fechar(...)
        +Remover(caminho, clock)
        +Listar(caminho)
    }
    class No
    class DescritorAberto

    class Processo {
        +int Id
        +long Chegada
        +TodasThreadsFinalizadas() bool
    }
    class ThreadSimulada {
        +EstadoThread Estado
        +IOperacao OperacaoAtual
        +string Identificacao
    }
    class Pcb
    class Tcb
    class IOperacao {
        <<interface>>
        +Resumo() string
    }
    class SurtoCpu
    class OperacaoES
    class OperacaoArquivo

    class LeitorDeCarga {
        +LerArquivo(caminho) Carga
        +LerTexto(texto) Carga
    }
    class Carga

    FabricaSimulador ..> NucleoSimulacao : cria
    FabricaSimulador ..> Carga : usa
    LeitorDeCarga ..> Carga : produz

    NucleoSimulacao *-- FilaDeEventos
    NucleoSimulacao o-- IEscalonador
    NucleoSimulacao o-- GerenciadorMemoria
    NucleoSimulacao o-- SistemaDeArquivos
    NucleoSimulacao o-- Dispositivo
    NucleoSimulacao o-- ColetorMetricas
    NucleoSimulacao o-- RegistradorEventos
    NucleoSimulacao o-- Processo
    FilaDeEventos o-- IEvento

    IEscalonador <|.. EscalonadorFcfs
    IEscalonador <|.. EscalonadorRoundRobin
    IEscalonador <|.. EscalonadorPrioridade

    GerenciadorMemoria o-- IPoliticaSubstituicao
    IPoliticaSubstituicao <|.. SubstituicaoFifo
    IPoliticaSubstituicao <|.. SubstituicaoLru
    GerenciadorMemoria ..> TabelaDePaginas : traduz

    Dispositivo o-- IEscalonadorDisco
    IEscalonadorDisco <|.. DiscoFcfs
    IEscalonadorDisco <|.. DiscoSstf
    IEscalonadorDisco <|.. DiscoScan

    SistemaDeArquivos *-- No
    SistemaDeArquivos o-- DescritorAberto

    Processo *-- Pcb
    Processo *-- ThreadSimulada
    ThreadSimulada *-- Tcb
    ThreadSimulada o-- IOperacao
    IOperacao <|.. SurtoCpu
    IOperacao <|.. OperacaoES
    IOperacao <|.. OperacaoArquivo
    Pcb *-- TabelaDePaginas
```

## Legenda das relações

- `*--` composição (o todo é dono da parte);
- `o--` agregação/associação (referência a uma parte de vida independente);
- `..>` dependência (uso pontual);
- `<|..` realização de interface.
