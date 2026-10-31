@echo off
REM ReleaseLine: start the local server. Usage: run.bat [--port N] [--no-browser]
cd /d "%~dp0"
where py >nul 2>nul
if %errorlevel%==0 (
  py -3 -m releaseline serve %*
) else (
  python -m releaseline serve %*
)
