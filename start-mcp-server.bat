@echo off
REM Build the project quietly and run the compiled binary to avoid stdout pollution
cd /d "%~dp0"
dotnet build --configuration Release --verbosity quiet > nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Build failed >&2
    exit /b 1
)
REM Run the compiled binary directly (not dotnet run) to avoid build output
".\bin\Release\net9.0\RhinoMcpServer.exe"