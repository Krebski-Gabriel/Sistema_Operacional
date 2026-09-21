#!/usr/bin/env bash
# Executa os tres experimentos exigidos pelo enunciado usando a CLI compilada.
# Salva o relatorio completo de cada execucao e monta tabelas comparativas.
#
# Uso: bash experimentos/rodar_experimentos.sh
set -euo pipefail

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
DLL="$RAIZ/src/SimuladorSO.Cli/bin/Release/net8.0/simulador.dll"
SAIDA="$RAIZ/experimentos"
CARGAS="$RAIZ/cargas"

if [ ! -f "$DLL" ]; then
  echo "Compilando CLI..."
  dotnet build "$RAIZ/src/SimuladorSO.Cli/SimuladorSO.Cli.csproj" -c Release >/dev/null
fi

sim() { dotnet "$DLL" "$@" --sem-log; }

# Extrai metricas do relatorio textual para uma linha de tabela.
linha() {
  local rotulo="$1"; shift
  local rel; rel="$(sim "$@")"
  local tf util trocas faltas taxa ret esp resp
  tf=$(echo "$rel"     | awk -F'\\.\\.\\.* ' '/Tempo total simulado/ {print $2}')
  util=$(echo "$rel"   | awk -F'\\.\\.\\.* ' '/Utilizacao da CPU/ {print $2}')
  trocas=$(echo "$rel" | awk -F'\\.\\.\\.* ' '/^Trocas de contexto/ {print $2}')
  faltas=$(echo "$rel" | grep 'Referencias a memoria' | sed 's/.*faltas=\([0-9]*\).*/\1/')
  taxa=$(echo "$rel"   | awk -F'\\.\\.\\.* ' '/Taxa de faltas/ {print $2}')
  ret=$(echo "$rel"    | awk '/^MEDIA/ {print $2}')
  esp=$(echo "$rel"    | awk '/^MEDIA/ {print $3}')
  resp=$(echo "$rel"   | awk '/^MEDIA/ {print $4}')
  printf "%-18s | tf=%-4s | ret=%-7s | esp=%-7s | resp=%-6s | util=%-8s | trocas=%-3s | faltas=%-3s | taxa=%s\n" \
    "$rotulo" "$tf" "$ret" "$esp" "$resp" "$util" "$trocas" "$faltas" "$taxa"
}

echo "=== Experimento 1: FCFS vs Round Robin vs Prioridade (carga mista) ===" | tee "$SAIDA/exp1_escalonadores.txt"
{
  linha "FCFS"        --carga "$CARGAS/mista.txt" --escalonador fcfs
  linha "RoundRobin q=4" --carga "$CARGAS/mista.txt" --escalonador rr --quantum 4
  linha "Prioridade (P)" --carga "$CARGAS/mista.txt" --escalonador prioridade --preemptivo
  linha "Prioridade (NP)" --carga "$CARGAS/mista.txt" --escalonador prioridade --nao-preemptivo
} | tee -a "$SAIDA/exp1_escalonadores.txt"
sim --carga "$CARGAS/mista.txt" --escalonador fcfs > "$SAIDA/exp1_fcfs_completo.txt"
sim --carga "$CARGAS/mista.txt" --escalonador rr --quantum 4 > "$SAIDA/exp1_rr_completo.txt"
sim --carga "$CARGAS/mista.txt" --escalonador prioridade --preemptivo > "$SAIDA/exp1_prioridade_completo.txt"

echo | tee -a "$SAIDA/exp1_escalonadores.txt"
echo "=== Experimento 2: variacao de quantum no Round Robin (carga mista) ===" | tee "$SAIDA/exp2_quantum.txt"
{
  for q in 1 2 4 8 16; do
    linha "RR quantum=$q" --carga "$CARGAS/mista.txt" --escalonador rr --quantum "$q"
  done
} | tee -a "$SAIDA/exp2_quantum.txt"

echo | tee -a "$SAIDA/exp2_quantum.txt"
echo "=== Experimento 3a: substituicao FIFO vs LRU (carga padrao_memoria, pagina=1) ===" | tee "$SAIDA/exp3_memoria.txt"
{
  for mol in 3 4; do
    linha "FIFO molduras=$mol" --carga "$CARGAS/padrao_memoria.txt" --escalonador fcfs --tamanho-pagina 1 --molduras "$mol" --substituicao fifo
    linha "LRU  molduras=$mol" --carga "$CARGAS/padrao_memoria.txt" --escalonador fcfs --tamanho-pagina 1 --molduras "$mol" --substituicao lru
  done
} | tee -a "$SAIDA/exp3_memoria.txt"

echo | tee -a "$SAIDA/exp3_memoria.txt"
# Linha especifica para disco: inclui o deslocamento do cabecote, metrica chave.
linha_disco() {
  local rotulo="$1"; local pol="$2"
  local rel; rel="$(sim --carga "$CARGAS/io_disco.txt" --escalonador fcfs --escalonador-disco "$pol")"
  local tf desloc atend ocup
  tf=$(echo "$rel" | awk -F'\\.\\.\\.* ' '/Tempo total simulado/ {print $2}')
  local linhadisco; linhadisco=$(echo "$rel" | awk '/^disco / {print}')
  atend=$(echo "$linhadisco" | awk '{print $4}')
  ocup=$(echo "$linhadisco"  | awk '{print $5}')
  desloc=$(echo "$linhadisco" | awk '{print $NF}')
  printf "%-12s | tf=%-4s | atendidas=%-3s | ocupado=%-3s | deslocamento_cabecote=%s\n" \
    "$rotulo" "$tf" "$atend" "$ocup" "$desloc"
}
echo "=== Experimento 3b: escalonamento de disco FCFS vs SSTF vs SCAN (carga io_disco) ===" | tee "$SAIDA/exp3_disco.txt"
{
  linha_disco "Disco FCFS" fcfs
  linha_disco "Disco SSTF" sstf
  linha_disco "Disco SCAN" scan
} | tee -a "$SAIDA/exp3_disco.txt"
sim --carga "$CARGAS/io_disco.txt" --escalonador fcfs --escalonador-disco fcfs > "$SAIDA/exp3_disco_fcfs_completo.txt"
sim --carga "$CARGAS/io_disco.txt" --escalonador fcfs --escalonador-disco sstf > "$SAIDA/exp3_disco_sstf_completo.txt"
sim --carga "$CARGAS/io_disco.txt" --escalonador fcfs --escalonador-disco scan > "$SAIDA/exp3_disco_scan_completo.txt"

echo
echo "Experimentos concluidos. Resultados em $SAIDA/"
