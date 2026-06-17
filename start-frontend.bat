@echo off
REM ── Starts the Vue frontend on http://localhost:5173 ──
REM Uses the portable Node install (same as your other project).
set "PATH=C:\Users\Kazmi\nodejs-portable\node-v22.14.0-win-x64;%PATH%"
cd /d "%~dp0web"
if not exist node_modules (
  echo Installing frontend packages, please wait...
  call npm install
)
echo Starting frontend on http://localhost:5173  (Ctrl+C to stop)
call npm run dev
pause
