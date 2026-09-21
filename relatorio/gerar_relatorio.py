# -*- coding: utf-8 -*-
"""Gera o relatorio tecnico do Simulador de SO em PDF (reportlab/Platypus)."""
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib import colors
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_JUSTIFY, TA_CENTER
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, ListFlowable, ListItem
)

AZUL = colors.HexColor("#1f3b57")
CINZA = colors.HexColor("#eef2f6")

styles = getSampleStyleSheet()
styles.add(ParagraphStyle("Corpo", parent=styles["Normal"], alignment=TA_JUSTIFY,
                          fontSize=10.5, leading=15, spaceAfter=6))
styles.add(ParagraphStyle("H1x", parent=styles["Heading1"], textColor=AZUL,
                          fontSize=16, spaceBefore=10, spaceAfter=8))
styles.add(ParagraphStyle("H2x", parent=styles["Heading2"], textColor=AZUL,
                          fontSize=12.5, spaceBefore=8, spaceAfter=5))
styles.add(ParagraphStyle("Titulo", parent=styles["Title"], textColor=AZUL, fontSize=22))
styles.add(ParagraphStyle("Sub", parent=styles["Normal"], alignment=TA_CENTER,
                          fontSize=12, textColor=colors.HexColor("#555555")))
styles.add(ParagraphStyle("Cap", parent=styles["Normal"], fontSize=9,
                          textColor=colors.HexColor("#666666"), spaceAfter=10))
styles.add(ParagraphStyle("Cod", parent=styles["Code"], fontSize=9, leading=12,
                          backColor=CINZA, leftIndent=6, spaceAfter=6))

S = styles
story = []

def p(txt, st="Corpo"): story.append(Paragraph(txt, S[st]))
def h1(txt): story.append(Paragraph(txt, S["H1x"]))
def h2(txt): story.append(Paragraph(txt, S["H2x"]))
def sp(h=6): story.append(Spacer(1, h))
def bullets(itens):
    story.append(ListFlowable(
        [ListItem(Paragraph(i, S["Corpo"]), leftIndent=10) for i in itens],
        bulletType="bullet", start="•"))
    sp(4)

def tabela(dados, larguras, cap=None):
    t = Table(dados, colWidths=larguras, repeatRows=1)
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), AZUL),
        ("TEXTCOLOR", (0, 0), (-1, 0), colors.white),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, -1), 8.8),
        ("ALIGN", (1, 0), (-1, -1), "CENTER"),
        ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [colors.white, CINZA]),
        ("GRID", (0, 0), (-1, -1), 0.4, colors.HexColor("#c7d0d9")),
        ("TOPPADDING", (0, 0), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 4),
    ]))
    story.append(t)
    if cap:
        story.append(Paragraph(cap, S["Cap"]))
    else:
        sp(8)

# ------------------------------------------------------------------ CAPA
story.append(Spacer(1, 3.2 * cm))
p("Relatório Técnico", "Titulo")
sp(6)
p("Simulador Didático de Sistemas Operacionais", "Sub")
sp(2)
p("Simulação orientada a eventos discretos — processos e threads, escalonamento de "
  "CPU, memória paginada, E/S e sistema de arquivos", "Sub")
sp(40)
p("Disciplina de Sistemas Operacionais<br/>Prof. Maurício Acconcia Dias", "Sub")
sp(20)
p("Implementação em C# / .NET 8", "Sub")
story.append(PageBreak())

# ------------------------------------------------------------------ 1
h1("1. Introdução e objetivo")
p("Este trabalho implementa um simulador didático que reproduz, em nível de modelo, "
  "o comportamento dos principais subsistemas de um sistema operacional. O objetivo "
  "não é obter desempenho real, mas <b>observar e medir</b> o efeito de diferentes "
  "políticas — de escalonamento de CPU, de substituição de páginas e de "
  "escalonamento de disco — sobre métricas clássicas como tempo de retorno, tempo de "
  "espera, utilização da CPU e taxa de faltas de página.")
p("Não há paralelismo real: processos, threads, dispositivos e o próprio processador "
  "são <b>objetos de modelo</b>. O tempo avança por um <b>relógio lógico</b>, em "
  "unidades abstratas (u.t.), controlado por uma fila de eventos futuros. Toda a "
  "lógica de domínio está na biblioteca <font face='Courier'>SimuladorSO.Nucleo</font>; "
  "a aplicação de linha de comando <font face='Courier'>SimuladorSO.Cli</font> "
  "apenas lê parâmetros, monta a simulação e imprime o relatório.")

h1("2. Arquitetura geral")
p("O coração do simulador é uma <b>fila de eventos</b> (min-heap) ordenada por "
  "(instante, sequência). O campo de sequência é um contador monotônico que garante "
  "<b>desempate determinístico</b> entre eventos agendados para o mesmo instante: a "
  "mesma carga, com a mesma configuração e a mesma semente, produz sempre o mesmo log "
  "e as mesmas métricas. Essa propriedade é verificada por teste automatizado.")
p("Os tipos de evento são: chegada de processo, despacho, execução de uma unidade de "
  "CPU, fim de atendimento de falta de página e conclusão de E/S. O núcleo consome o "
  "evento de menor tempo, avança o relógio até ele e o executa; a execução pode "
  "agendar novos eventos. O laço termina quando a fila esvazia.")
p("<b>Separação mecanismo × política.</b> O núcleo (mecanismo) não conhece as "
  "políticas concretas. Ele depende apenas de três interfaces, cujas implementações "
  "são injetadas pela <font face='Courier'>FabricaSimulador</font>:")
bullets([
    "<font face='Courier'>IEscalonador</font> — FCFS, Round Robin e Prioridade;",
    "<font face='Courier'>IPoliticaSubstituicao</font> — FIFO e LRU;",
    "<font face='Courier'>IEscalonadorDisco</font> — FCFS, SSTF e SCAN.",
])
p("Trocar de política é trocar a implementação injetada, sem alterar o núcleo — o que "
  "mantém baixo o acoplamento e facilita a extensão.")

h1("3. Processos e threads")
p("A <b>thread</b> é a unidade escalonável. Cada thread carrega um fluxo sequencial de "
  "operações (surtos de CPU, operações de E/S e operações de arquivo) e possui um TCB "
  "(contador de programa lógico, registradores e pilha lógica). O processo agrupa "
  "threads e mantém o PCB (prioridade, espaço de endereçamento — a tabela de páginas — "
  "e a tabela de arquivos abertos). Um processo termina quando <b>todas</b> as suas "
  "threads terminam.")
p("Os estados da thread e todas as transições são registrados no log:")
tabela(
    [["Transição", "Evento que a provoca"],
     ["Nova → Pronta", "Admissão na chegada do processo"],
     ["Pronta → Executando", "Despacho pelo escalonador"],
     ["Executando → Pronta", "Preempção (fim de quantum ou prioridade maior)"],
     ["Executando → Bloqueada", "E/S bloqueante ou falta de página"],
     ["Bloqueada → Pronta", "Interrupção de conclusão de E/S / fim do atendimento da falta"],
     ["Executando → Finalizada", "Fim da última operação da thread"]],
    [6.5 * cm, 9.5 * cm],
    "Tabela 1 — Transições de estado das threads.")

h1("4. Escalonamento de CPU")
p("A CPU executa em <b>passos unitários</b>: cada unidade de tempo consumida por um "
  "surto é um evento. Essa granularidade permite que conclusões concorrentes (por "
  "exemplo, o fim do atendimento de uma falta de página de outra thread) sejam "
  "ordenadas corretamente e que o quantum e a preempção sejam avaliados em fronteiras "
  "bem definidas.")
p("<b>Troca de contexto.</b> A cada despacho é cobrado um custo configurável "
  "(<font face='Courier'>--custo-troca</font>), durante o qual a CPU fica ocupada. "
  "Esse tempo é contabilizado como <b>sobrecarga</b>, separado da fração útil; o "
  "número de trocas também é medido. A ociosidade é o que sobra: "
  "ocioso = total − útil − sobrecarga.")
p("<b>Políticas.</b> FCFS é não preemptivo. Round Robin usa quantum configurável e "
  "preempta ao esgotá-lo. A política de Prioridade admite os modos preemptivo e não "
  "preemptivo (declarados na linha de comando) e possui <i>aging</i> opcional como "
  "extensão. Menor número indica maior prioridade. O <b>desempate</b> é sempre "
  "determinístico: prioridade efetiva, depois instante de chegada e, por fim, a ordem "
  "de identificação da thread.")

h1("5. Gerência de memória")
p("Cada processo tem um espaço de endereçamento lógico próprio e uma <b>tabela de "
  "páginas</b> (bit de presença e moldura). Um endereço lógico é traduzido em "
  "(página = endereço / tamanho_da_página, deslocamento = endereço mod tamanho). No "
  "acesso, se a página está presente conta-se um <b>acerto</b>; caso contrário, uma "
  "<b>falta</b>: a thread <b>bloqueia</b> durante o tempo de atendimento e, ao final, "
  "a página é carregada. Havendo moldura livre, ela é usada; caso contrário, a "
  "política de substituição escolhe a vítima. FIFO (mínimo obrigatório) e LRU estão "
  "implementadas. Não há espera ocupada: o desbloqueio ocorre por evento.")

h1("6. Entrada e saída")
p("Há dispositivos de <b>bloco</b> (disco) e de <b>caractere</b> (console). Cada "
  "dispositivo tem fila própria, estado (livre/ocupado) e tempo de serviço "
  "configurável. Numa operação <b>bloqueante</b>, a thread suspende até a "
  "<b>interrupção</b> de conclusão devolvê-la à fila de prontos; a versão <b>não "
  "bloqueante</b> não suspende a thread. Não há espera ocupada em nenhum caso.")
p("O disco suporta três políticas de escalonamento: FCFS (mínimo), SSTF e SCAN "
  "(elevador). O simulador mede o <b>deslocamento total do cabeçote</b>, que serve de "
  "proxy para o custo mecânico de busca e é a métrica comparada no Experimento 3.")

h1("7. Sistema de arquivos")
p("O sistema de arquivos é <b>hierárquico</b> a partir da raiz. Cada nó (arquivo ou "
  "diretório) tem um descritor com nome, tipo, tamanho, permissões (leitura/escrita), "
  "timestamps lógicos e a localização dos blocos. As operações suportadas são criar "
  "arquivo/diretório, abrir, ler, escrever, fechar, remover e listar. Ao abrir, cria-se "
  "um <b>descritor por processo</b> ligado a uma tabela global de arquivos abertos; "
  "fechar libera o descritor.")
p("Operações inválidas são detectadas e registradas como erro, sem corromper o estado: "
  "ler por um descritor fechado, remover um caminho inexistente, remover um arquivo "
  "aberto, recriar um nó existente e violações de permissão.")
p("<b>Alocação encadeada.</b> Os blocos de um arquivo formam uma lista ligada. A "
  "vantagem é não sofrer fragmentação externa e crescer sem realocação. A <b>limitação "
  "documentada</b> é o acesso aleatório ineficiente: alcançar o bloco <i>k</i> exige "
  "percorrer a cadeia desde o início, tornando o acesso essencialmente sequencial.")

h1("8. Métricas")
p("Todas as métricas derivam do relógio lógico. As fórmulas usadas são:")
bullets([
    "<b>Tempo de retorno</b> (turnaround) = instante de término − instante de chegada;",
    "<b>Tempo de espera</b> = tempo acumulado na fila de prontos;",
    "<b>Tempo de resposta</b> = instante da primeira ida à CPU − chegada;",
    "<b>Utilização da CPU</b> = unidades úteis / tempo total (a sobrecarga de troca é "
    "reportada à parte);",
    "<b>Vazão</b> (throughput) = processos concluídos / tempo total;",
    "<b>Utilização de dispositivo</b> = tempo ocupado / tempo total;",
    "<b>Taxa de faltas</b> = faltas / total de referências;",
    "<b>Trocas de contexto</b> = contagem + sobrecarga total acumulada.",
])

story.append(PageBreak())

# ------------------------------------------------------------------ EXPERIMENTOS
h1("9. Experimentos")
p("Todos os experimentos são reproduzíveis por "
  "<font face='Courier'>bash experimentos/rodar_experimentos.sh</font>. O custo de "
  "troca de contexto foi mantido no padrão (1 u.t.) e a semente fixada, garantindo "
  "resultados idênticos entre execuções.")

h2("9.1 Experimento 1 — FCFS × Round Robin × Prioridade (carga mista)")
p("Mesma carga (<font face='Courier'>cargas/mista.txt</font>, quatro threads em três "
  "processos, com CPU, E/S e arquivos) submetida às três políticas. Round Robin com "
  "quantum 4; Prioridade nos modos preemptivo (P) e não preemptivo (NP).")
tabela(
    [["Política", "T. total", "Retorno méd.", "Espera méd.", "Resposta méd.", "Util. CPU", "Trocas"],
     ["FCFS", "78", "55,00", "12,75", "1,25", "41,03%", "27"],
     ["Round Robin q=4", "78", "56,25", "13,50", "1,25", "41,03%", "28"],
     ["Prioridade (P)", "92", "56,75", "14,75", "1,25", "34,78%", "28"],
     ["Prioridade (NP)", "84", "54,75", "12,75", "1,25", "38,10%", "27"]],
    [3.6 * cm, 1.7 * cm, 2.3 * cm, 2.1 * cm, 2.3 * cm, 1.9 * cm, 1.5 * cm],
    "Tabela 2 — Comparação de escalonadores de CPU sobre a carga mista.")
p("<b>Interpretação.</b> Como a carga tem muita E/S, as threads cedem a CPU "
  "frequentemente por conta própria, e FCFS e Round Robin ficam quase empatados — o "
  "quantum 4 raramente é atingido, gerando só uma preempção a mais. A Prioridade "
  "preemptiva apresenta o pior tempo total e a menor utilização: as preempções por "
  "chegada de thread mais prioritária aumentam o número de trocas e, com isso, a "
  "sobrecarga, esticando o tempo total para 92 u.t. O modo não preemptivo recupera "
  "parte disso. A lição é clara: preempção melhora a responsividade a processos "
  "prioritários, mas cobra o preço da troca de contexto.")

h2("9.2 Experimento 2 — variação do quantum no Round Robin (carga mista)")
p("A mesma carga mista sob Round Robin, variando o quantum de 1 a 16 u.t.")
tabela(
    [["Quantum", "T. total", "Retorno méd.", "Espera méd.", "Util. CPU", "Trocas"],
     ["1", "101", "75,75", "33,75", "31,68%", "52"],
     ["2", "79", "57,25", "15,00", "40,51%", "30"],
     ["4", "78", "56,25", "13,50", "41,03%", "28"],
     ["8", "78", "55,00", "12,75", "41,03%", "27"],
     ["16", "78", "55,00", "12,75", "41,03%", "27"]],
    [2.4 * cm, 2.2 * cm, 2.7 * cm, 2.6 * cm, 2.3 * cm, 2.0 * cm],
    "Tabela 3 — Efeito do quantum sobre o Round Robin.")
p("<b>Interpretação.</b> Um quantum muito pequeno (1) é desastroso: são 52 trocas de "
  "contexto, a utilização da CPU cai para 31,68% e o tempo total sobe para 101 u.t., "
  "pois a sobrecarga de troca domina. À medida que o quantum cresce, o número de "
  "trocas diminui e o desempenho melhora; a partir de um quantum grande o "
  "comportamento <b>converge para o FCFS</b> (quantum 8 e 16 já são idênticos entre si "
  "e iguais ao FCFS da Tabela 2), porque as fatias passam a ser maiores do que os "
  "surtos de CPU. O quantum é, portanto, um compromisso entre responsividade e "
  "sobrecarga.")

h2("9.3 Experimento 3 — memória (FIFO × LRU) e disco (FCFS × SSTF × SCAN)")
p("<b>Parte A — substituição de páginas.</b> Carga "
  "<font face='Courier'>cargas/padrao_memoria.txt</font>, com tamanho de página 1 e um "
  "conjunto de trabalho fixo {1, 2} reutilizado a cada ciclo, mais uma terceira página "
  "que sempre muda. Comparam-se FIFO e LRU com 3 e 4 molduras.")
tabela(
    [["Política", "Molduras", "Faltas", "Taxa de faltas", "T. total", "Util. CPU"],
     ["FIFO", "3", "11", "64,71%", "95", "17,89%"],
     ["LRU", "3", "7", "41,18%", "67", "25,37%"],
     ["FIFO", "4", "9", "52,94%", "81", "20,99%"],
     ["LRU", "4", "7", "41,18%", "67", "25,37%"]],
    [2.6 * cm, 2.2 * cm, 1.8 * cm, 3.0 * cm, 2.2 * cm, 2.2 * cm],
    "Tabela 4 — FIFO × LRU sobre um padrão com reutilização.")
p("<b>Interpretação.</b> Com 3 molduras, LRU sofre 7 faltas contra 11 do FIFO — uma "
  "redução de ~36%. Como a thread faltante bloqueia durante o atendimento, menos faltas "
  "significam menos tempo bloqueado: o tempo total cai de 95 para 67 u.t. e a "
  "utilização da CPU sobe. O motivo é que LRU preserva as páginas 1 e 2 (sempre as mais "
  "recentemente usadas) e só troca a moldura rotativa, ao passo que FIFO, ignorando a "
  "reutilização, expulsa 1 e 2 periodicamente. Vale notar que o resultado depende do "
  "padrão de acesso: em sequências sem reutilização as duas políticas podem empatar.")
sp(4)
p("<b>Parte B — escalonamento de disco.</b> Carga "
  "<font face='Courier'>cargas/io_disco.txt</font>: cinco requisições chegam à fila do "
  "disco praticamente juntas (posições 20, 190, 50, 160, 90; cabeçote inicia em 0), "
  "permitindo o reordenamento.")
tabela(
    [["Política", "T. total", "Requisições", "Desloc. do cabeçote"],
     ["FCFS", "43", "5", "510"],
     ["SSTF", "43", "5", "190"],
     ["SCAN", "43", "5", "190"]],
    [3.2 * cm, 2.6 * cm, 3.2 * cm, 4.4 * cm],
    "Tabela 5 — FCFS × SSTF × SCAN: deslocamento do cabeçote.")
p("<b>Interpretação.</b> FCFS atende na ordem de chegada e percorre 510 unidades; SSTF "
  "e SCAN, ao reordenarem para reduzir o movimento, percorrem apenas 190 — uma redução "
  "de ~63%. O tempo total permanece 43 u.t. nas três porque, neste modelo abstrato, o "
  "tempo de serviço é fixo e independente da distância, e o disco (totalmente ocupado) "
  "é o gargalo. Num modelo em que o tempo de serviço fosse proporcional ao "
  "deslocamento, essa economia de movimento se traduziria também em menor tempo total.")

story.append(PageBreak())

h1("10. Testes automatizados")
p("A suíte é executada com "
  "<font face='Courier'>dotnet run --project tests/SimuladorSO.Testes -c Release</font> "
  "e usa um arcabouço de asserções próprio, sem dependências externas (roda offline). "
  "São 63 verificações, todas passando, cobrindo memória (tradução de endereços, falta "
  "seguida de acerto, vítimas FIFO e LRU), escalonamento (ordem FCFS, fim de quantum, "
  "seleção e preempção por prioridade), sistema de arquivos (ciclo completo, "
  "descritores e posição, operações inválidas, permissões, diretórios) e integração "
  "(métricas conferidas à mão, transições de estado com E/S, contagem de preempções e "
  "determinismo). O processo retorna código 0 em sucesso e 1 em falha, adequado a CI.")

h1("11. Limitações")
bullets([
    "<b>Alocação encadeada</b> no sistema de arquivos: acesso aleatório exige percorrer "
    "a cadeia, ficando essencialmente sequencial.",
    "<b>Modelo de tempo abstrato</b>: o tempo de serviço do disco não depende do "
    "deslocamento do cabeçote, o que limita o impacto de SSTF/SCAN no tempo total "
    "(embora o deslocamento seja corretamente medido).",
    "<b>Passo unitário de CPU</b>: preempções e verificações ocorrem em fronteiras de "
    "unidade, introduzindo latência de no máximo uma unidade — coerente com a "
    "granularidade do relógio lógico.",
    "<b>Custo de troca a cada despacho</b>: a sobrecarga é cobrada em todo despacho, "
    "inclusive no retorno de bloqueio, o que é uma simplificação conservadora.",
    "<b>Sem paralelismo real</b>: há uma única CPU; múltiplos núcleos não são "
    "modelados.",
])

h1("12. Conclusão")
p("O simulador cumpre os requisitos do enunciado: modela processos e threads com "
  "transições de estado registradas, três políticas de escalonamento de CPU com troca "
  "de contexto contabilizada, memória paginada com FIFO e LRU, E/S bloqueante e não "
  "bloqueante com dispositivos de bloco e caractere e três políticas de disco, além de "
  "um sistema de arquivos hierárquico com detecção de operações inválidas. Os "
  "experimentos confirmam efeitos teóricos conhecidos — o compromisso do quantum, o "
  "custo da preempção, a vantagem do LRU sob reutilização e a economia de movimento do "
  "SSTF/SCAN — todos medidos a partir do relógio lógico. A arquitetura orientada a "
  "objetos, com clara separação entre mecanismo e política, torna o sistema fácil de "
  "estender e de testar, como evidenciam as 63 verificações automatizadas e a "
  "reprodutibilidade determinística das execuções.")

doc = SimpleDocTemplate(
    "/home/claude/SimuladorSO/relatorio/relatorio.pdf", pagesize=A4,
    leftMargin=2.2 * cm, rightMargin=2.2 * cm, topMargin=2.0 * cm, bottomMargin=2.0 * cm,
    title="Relatorio Tecnico - Simulador de SO")
doc.build(story)
print("PDF gerado.")
