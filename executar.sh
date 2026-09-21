#!/usr/bin/env bash
# =====================================================================
#  Simulador Didatico de Sistemas Operacionais - inicializador
#  Linux / macOS
#
#  Uso:
#    ./executar.sh              -> compila e abre o modo guiado (menu)
#    ./executar.sh testes       -> roda os testes automatizados
#    ./executar.sh experimentos -> roda os tres experimentos do relatorio
#    ./executar.sh <opcoes>     -> repassa as opcoes para o simulador
#                                  ex.: ./executar.sh --carga mista --escalonador rr
# =====================================================================
set -euo pipefail
cd "$(dirname "$0")"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# --- 1. Verifica o .NET ---------------------------------------------
if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERRO: o .NET SDK 8.0 nao foi encontrado."
  echo
  echo "Instale com uma das opcoes abaixo e tente novamente:"
  echo "  Ubuntu/Debian : sudo apt-get update && sudo apt-get install -y dotnet-sdk-8.0"
  echo "  macOS (brew)  : brew install --cask dotnet-sdk"
  echo "  Outros        : https://dotnet.microsoft.com/download/dotnet/8.0"
  exit 1
fi

# --- 2. Compila (silencioso; mostra tudo se falhar) ------------------
echo "Compilando o projeto..."
if ! dotnet build -c Release --nologo -v quiet >/tmp/simulador_build.log 2>&1; then
  echo "ERRO ao compilar. Saida completa:"
  cat /tmp/simulador_build.log
  exit 1
fi
echo "Compilacao concluida."
echo

# --- 3. Decide o que executar ----------------------------------------
case "${1:-}" in
  testes|teste|test)
    exec dotnet run --project tests/SimuladorSO.Testes -c Release --no-build
    ;;
  experimentos|experimento)
    exec bash experimentos/rodar_experimentos.sh
    ;;
  *)
    exec dotnet run --project src/SimuladorSO.Cli -c Release --no-build -- "$@"
    ;;
esac
