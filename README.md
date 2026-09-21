# Simulador Didático de Sistemas Operacionais

Simulador orientado a eventos discretos que modela, de forma didática, os
principais subsistemas de um sistema operacional: **processos e threads**,
**escalonamento de CPU**, **gerência de memória paginada**, **dispositivos de
E/S** e um **sistema de arquivos hierárquico**. Não há paralelismo real: todas
as entidades são objetos de modelo e o tempo avança por um **relógio lógico**
em unidades abstratas, governado por uma fila de eventos.

O projeto foi escrito em **C# / .NET 8**, usando apenas a biblioteca padrão
(BCL), sem dependências externas — compila e roda em ambiente limpo e offline.

---

## ⚡ Início rápido

Precisa apenas do **.NET SDK 8.0** instalado. Na pasta do projeto:

**Linux / macOS**

```bash
./executar.sh
```

**Windows** — dê um duplo clique em **`executar.bat`** (ou rode no terminal):

```bat
executar.bat
```

O script compila o projeto e abre o **menu principal**:

```
=================== MENU PRINCIPAL ===================
  [1] Executar uma simulacao
  [2] Listar cargas de trabalho disponiveis
  [3] Ver ajuda e parametros de linha de comando
  [0] Encerrar
======================================================
```

O programa **fica em laço**: depois de cada simulação ele volta ao menu, para
que você rode quantas configurações quiser (comparar FCFS com Round Robin, por
exemplo) sem reiniciar nada. Ele só termina quando você escolhe **`[0]`
Encerrar**.

Ao escolher `[1]`, você define a carga, a política de escalonamento e o destino
do log respondendo perguntas simples — sem decorar nenhum parâmetro. Ao final,
o menu mostra o *comando equivalente*, caso queira repetir a execução direto
pelo terminal.

Outros atalhos do mesmo script:

```bash
./executar.sh testes         # roda os testes automatizados
./executar.sh experimentos   # roda os três experimentos do relatório
./executar.sh --carga mista --escalonador rr --quantum 2   # repassa opções
```

> Prefere ver as opções antes? `./executar.sh --ajuda` ou
> `./executar.sh --listar-cargas`.

---

## 1. Requisitos

- **.NET SDK 8.0** (testado com 8.0.131).
- Sistema operacional Linux, macOS ou Windows. As instruções abaixo usam Ubuntu.

Verifique a instalação com:

```bash
dotnet --version
```

## 2. Instalação do ambiente (Ubuntu 24.04)

```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

Em outras plataformas, siga o instalador oficial em <https://dotnet.microsoft.com/download>.

> O repositório inclui um `nuget.config` que **desativa fontes NuGet externas**.
> Como o projeto não usa pacotes de terceiros, a compilação funciona sem acesso
> à internet.

## 3. Compilação

Na raiz do repositório:

```bash
dotnet build -c Release
```

Isso compila os três projetos da solução:

- `src/SimuladorSO.Nucleo` — biblioteca com toda a lógica de domínio;
- `src/SimuladorSO.Cli` — aplicação de linha de comando (executável `simulador`);
- `tests/SimuladorSO.Testes` — testes automatizados.

## 4. Execução

Há três formas, da mais simples à mais direta.

**(a) Script de um clique** — recomendado (veja o *Início rápido* acima):

```bash
./executar.sh                 # Linux/macOS
executar.bat                  # Windows
```

**(b) Modo guiado por menu** — sem decorar parâmetros:

```bash
dotnet run --project src/SimuladorSO.Cli -c Release
# ou, explicitamente:
dotnet run --project src/SimuladorSO.Cli -c Release -- --interativo
```

Executar **sem nenhum argumento** já abre o menu. Para ver as cargas prontas:

```bash
dotnet run --project src/SimuladorSO.Cli -c Release -- --listar-cargas
```

**(c) Linha de comando direta:**

```bash
dotnet run --project src/SimuladorSO.Cli -c Release -- --carga mista
```

Tudo que vem depois de `--` são argumentos do simulador. O parâmetro `--carga`
aceita **atalho**: `--carga mista` equivale a `--carga cargas/mista.txt` — e
funciona mesmo que você esteja em outro diretório.

Por padrão o log completo dos eventos é impresso, seguido do relatório de
métricas. Para gravar o log em arquivo e ver apenas as métricas no terminal:

```bash
dotnet run --project src/SimuladorSO.Cli -c Release -- \
  --carga cargas/mista.txt --escalonador rr --quantum 4 --log saida.log
```

Também é possível executar diretamente o binário compilado:

```bash
dotnet src/SimuladorSO.Cli/bin/Release/net8.0/simulador.dll --carga cargas/mista.txt
```

### 4.1 Parâmetros da linha de comando

| Parâmetro | Descrição | Padrão |
|---|---|---|
| `--carga <arquivo>` | **Obrigatório.** Arquivo de carga de trabalho. | — |
| `--escalonador fcfs\|rr\|prioridade` | Política de escalonamento de CPU. | `fcfs` |
| `--quantum <n>` | Quantum do Round Robin (em u.t.). | `4` |
| `--custo-troca <n>` | Custo da troca de contexto (u.t. de CPU cobradas como sobrecarga a cada despacho). | `1` |
| `--preemptivo` / `--nao-preemptivo` | Declara se as prioridades são preemptivas. | preemptivo |
| `--envelhecimento` | Ativa *aging* nas prioridades (extensão). | desativado |
| `--intervalo-envelhecimento <n>` | Intervalo de espera para promover a prioridade. | `5` |
| `--tamanho-pagina <n>` | Tamanho da página (e da moldura). | `4` |
| `--molduras <n>` | Número de molduras físicas. | `8` |
| `--tempo-falta <n>` | Tempo de atendimento de uma falta de página. | `6` |
| `--substituicao fifo\|lru` | Política de substituição de páginas. | `fifo` |
| `--tempo-dispositivo <n>` | Tempo de serviço padrão dos dispositivos. | `5` |
| `--escalonador-disco fcfs\|sstf\|scan` | Política de escalonamento do disco. | `fcfs` |
| `--total-blocos <n>` | Blocos do disco simulado (sistema de arquivos). | `256` |
| `--tamanho-bloco <n>` | Tamanho do bloco. | `512` |
| `--semente <n>` | Semente de aleatoriedade (reprodutibilidade). | `0` |
| `--log <arquivo>` | Grava o log de eventos em arquivo. | terminal |
| `--sem-log` | Não imprime o log (apenas métricas). | — |
| `-i`, `--interativo` | Abre o modo guiado por menu. | — |
| `--listar-cargas` | Lista as cargas de trabalho prontas. | — |
| `-h`, `--ajuda` | Mostra a ajuda (com exemplos). | — |

Parâmetros inválidos produzem uma mensagem clara e o programa encerra **sem
processar** a simulação (código de saída ≠ 0). Uma diretiva `config semente=N`
dentro do arquivo de carga tem precedência sobre `--semente`.

## 5. Formato do arquivo de carga

A carga de trabalho é **externa** — trocar de carga **não exige recompilar**.
É um arquivo de texto, uma diretiva por linha; linhas em branco e iniciadas por
`#` são ignoradas. O parser é sensível ao contexto: linhas de operação
pertencem à última `thread` declarada.

```
# Configuração opcional
config semente=1

# Declaração explícita de dispositivo (opcional; são criados sob demanda)
disp disco bloco servico=6
disp console caractere servico=2

# Processo e suas threads
proc P1 chegada=0 prioridade=2
thread T1
  cpu dur=5 refs=0,4,8            # surto de CPU de 5 u.t. referenciando 3 endereços
  io dev=disco dur=8 pos=10       # E/S bloqueante no disco, posição 10
  fs criararq caminho=/a.txt      # operações de arquivo (instantâneas)
  fs abrir caminho=/a.txt handle=h1 modo=rw
  fs escrever handle=h1 tam=100
  fs fechar handle=h1
thread T2
  cpu dur=3
  io dev=console dur=4 tipo=naobloqueante
```

### Diretivas

- `config semente=<n>` — semente da simulação.
- `disp <nome> bloco|caractere [servico=<n>]` — declara um dispositivo. Se uma
  operação referenciar um dispositivo não declarado, ele é criado
  automaticamente (heurística: nomes como *disco/disk/hd/ssd* viram dispositivo
  de bloco). Há sempre ao menos um dispositivo de bloco (`disco`) e um de
  caractere (`console`).
- `proc <id> chegada=<n> [prioridade=<n>]` — inicia um processo. Threads
  declaradas em seguida pertencem a ele. Menor número de prioridade = maior
  prioridade. O identificador numérico é extraído do rótulo (`P1` → 1).
- `thread <id>` — declara uma thread do processo corrente. As operações
  seguintes formam o fluxo sequencial da thread.
- `cpu dur=<n> [refs=<a,b,c>]` — surto de CPU de `n` unidades. `refs` são
  endereços lógicos acessados durante o surto (traduzidos para páginas).
- `io dev=<nome> dur=<n> [tipo=bloqueante|naobloqueante] [pos=<n>]` — operação
  de E/S. A bloqueante suspende a thread até a interrupção de conclusão; a não
  bloqueante não suspende. `pos` é a posição no disco (para SSTF/SCAN).
- `fs <op> ...` — operações de sistema de arquivos (resolvidas em tempo lógico
  zero): `criararq`, `criardir`, `abrir`, `ler`, `escrever`, `fechar`,
  `remover`, `listar`.

Cargas prontas em `cargas/`:

| Arquivo | Perfil |
|---|---|
| `cpu_bound.txt` | Predominância de CPU. |
| `io_bound.txt` | Predominância de E/S. |
| `mista.txt` | Mista (CPU + E/S + arquivos + operação inválida demonstrativa). |
| `verificacao_pequena.txt` | Carga mínima, conferível à mão. |
| `padrao_memoria.txt` | Padrão de referências que evidencia FIFO × LRU. |
| `io_disco.txt` | Requisições concorrentes de disco (evidencia FCFS × SSTF × SCAN). |

## 6. Exemplos

```bash
# FCFS na carga mista
./executar.sh --carga mista --escalonador fcfs

# Round Robin com quantum 2 e custo de troca 1
./executar.sh --carga mista --escalonador rr --quantum 2 --custo-troca 1

# Prioridade preemptiva com aging
./executar.sh --carga mista --escalonador prioridade --preemptivo --envelhecimento

# Estudo de memória: LRU com poucas molduras
./executar.sh --carga padrao_memoria --tamanho-pagina 1 --molduras 3 --substituicao lru

# Estudo de disco: SSTF, apenas métricas
./executar.sh --carga io_disco --escalonador-disco sstf --sem-log
```

## 7. Testes automatizados

Os testes usam um arcabouço de asserções próprio (sem bibliotecas externas),
então rodam offline. Para executar **toda** a suíte:

```bash
./executar.sh testes
# ou, sem o script:
dotnet run --project tests/SimuladorSO.Testes -c Release
```

**Resultado esperado:** a última linha deve ser
`RESULTADO: TODOS OS TESTES PASSARAM`, com `Falhas: 0` (63 verificações). O
código de saída é `0` em sucesso e `1` se houver qualquer falha (útil para CI).

Cobertura dos testes:

- **Memória** — tradução de endereços, falta seguida de acerto, vítimas FIFO e LRU.
- **Escalonamento** — ordem FCFS, fim de quantum no Round Robin, seleção e
  preempção por prioridade.
- **Arquivos** — ciclo criar/abrir/escrever/ler/fechar, descritores e posição,
  operações inválidas (abrir inexistente, ler descritor fechado, remover
  inexistente, remover aberto, duplicar), permissões e diretórios.
- **Integração** — métricas conferidas à mão (turnaround, espera, resposta),
  transições de estado com E/S bloqueante, contagem de preempções por quantum e
  determinismo (duas execuções idênticas produzem log e métricas idênticos).

## 8. Reprodução dos experimentos

```bash
bash experimentos/rodar_experimentos.sh
```

Gera, em `experimentos/`, as tabelas comparativas e os relatórios completos dos
três experimentos discutidos no relatório técnico (`relatorio/relatorio.pdf`):
escalonadores de CPU, variação de quantum e estudo de memória/disco.

## 9. Estrutura do repositório

```
SimuladorSO/
├── README.md
├── executar.sh                  # inicializador Linux/macOS (compila + menu guiado)
├── executar.bat                 # inicializador Windows
├── SimuladorSO.sln
├── nuget.config                 # desativa fontes NuGet externas (build offline)
├── diagrama_classes.md          # diagrama de classes (Mermaid)
├── src/
│   ├── SimuladorSO.Nucleo/      # domínio: processos, escalonamento, memória, E/S, arquivos
│   └── SimuladorSO.Cli/         # interface de linha de comando
├── tests/
│   └── SimuladorSO.Testes/      # testes unitários e de integração
├── cargas/                      # cargas de trabalho externas
├── experimentos/                # script e saídas dos experimentos
└── relatorio/                   # relatório técnico em PDF + diagrama de classes
```

## 10. Organização e decisões de projeto

- **Separação mecanismo × política:** o núcleo (mecanismo) desconhece a política
  concreta; escalonadores de CPU (`IEscalonador`), de disco (`IEscalonadorDisco`)
  e de substituição de páginas (`IPoliticaSubstituicao`) são intercambiáveis por
  injeção via `FabricaSimulador`.
- **Fila de eventos como única fonte do tempo:** a CPU executa em passos
  unitários, o que permite que conclusões de faltas e de E/S concorrentes sejam
  ordenadas de forma determinística (desempate por sequência monotônica).
- **Determinismo:** mesma carga + mesma configuração + mesma semente produzem
  exatamente o mesmo log e as mesmas métricas.

Detalhes de modelagem, formulário das métricas, resultados e limitações estão no
relatório técnico em `relatorio/relatorio.pdf`.
