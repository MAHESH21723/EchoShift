@echo off
cd /d "%~dp0"
if not exist "Builds\WebGL\index.html" (
    echo Build the browser version first using Build-Browser.cmd.
    pause
    exit /b 1
)
start "" "http://127.0.0.1:8765"
node serve.cjs
pause
