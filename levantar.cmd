@echo off
REM Doble clic: levanta base de datos + API + web en Docker y abre el navegador.
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0levantar.ps1"
pause
