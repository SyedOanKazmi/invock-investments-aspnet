@echo off
REM ── Starts the ASP.NET backend on http://127.0.0.1:8002 ──
cd /d "%~dp0api"
echo Starting ASP.NET backend on http://127.0.0.1:8002  (Ctrl+C to stop)
dotnet run --urls http://127.0.0.1:8002
pause
