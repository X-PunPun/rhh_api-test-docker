@echo off
rem Levanta todo y queda vigilando cambios del codigo (reconstruye automaticamente).
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0levantar.ps1" -Vigilar
pause
