@echo off
REM =====================================================================
REM  Simulador Didatico de Sistemas Operacionais - inicializador
REM  Windows
REM
REM  Uso:
REM    executar.bat              -> compila e abre o modo guiado (menu)
REM    executar.bat testes       -> roda os testes automatizados
REM    executar.bat experimentos -> roda os tres experimentos do relatorio
REM    executar.bat <opcoes>     -> repassa as opcoes para o simulador
REM                                 ex.: executar.bat --carga mista --escalonador rr
REM =====================================================================
setlocal
cd /d "%~dp0"

set DOTNET_CLI_TELEMETRY_OPTOUT=1
set DOTNET_NOLOGO=1

REM --- 1. Verifica o .NET ---------------------------------------------
where dotnet >nul 2>nul
if errorlevel 1 (
    echo ERRO: o .NET SDK 8.0 nao foi encontrado.
    echo.
    echo Baixe e instale em: https://dotnet.microsoft.com/download/dotnet/8.0
    echo Depois de instalar, FECHE e reabra esta janela.
    echo.
    pause
    exit /b 1
)

REM --- 2. Compila -------------------------------------------------------
echo Compilando o projeto...
dotnet build -c Release --nologo -v quiet
if errorlevel 1 (
    echo.
    echo ERRO ao compilar o projeto.
    pause
    exit /b 1
)
echo Compilacao concluida.
echo.

REM --- 3. Decide o que executar ----------------------------------------
if /i "%~1"=="testes" (
    dotnet run --project tests\SimuladorSO.Testes -c Release --no-build
    echo.
    pause
    exit /b 0
)
if /i "%~1"=="experimentos" (
    echo Os experimentos usam um script bash. No Windows, rode os comandos
    echo manualmente ou use o Git Bash / WSL:
    echo    bash experimentos/rodar_experimentos.sh
    echo.
    pause
    exit /b 0
)

REM No modo guiado (sem argumentos), o proprio simulador fica em laco ate o
REM usuario escolher "Encerrar", entao nao e preciso pausar ao final.
dotnet run --project src\SimuladorSO.Cli -c Release --no-build -- %*
if not "%~1"=="" (
    echo.
    pause
)
