# levantar.ps1 - Levanta el ambiente completo en Docker (base de datos + API + web) y abre el navegador.
# Uso: doble clic en levantar.cmd, o bien:  powershell -ExecutionPolicy Bypass -File .\levantar.ps1
#      Con -Vigilar (levantar-dev.cmd) queda observando cambios en el código y reconstruye solo.

param([switch]$Vigilar)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

function Paso($texto) { Write-Host "`n==> $texto" -ForegroundColor Cyan }

Paso 'Verificando Docker'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'No se encontró Docker. Instala Docker Desktop.' }
docker info *> $null
if ($LASTEXITCODE -ne 0) { throw 'Docker Desktop no está en ejecución. Ábrelo, espera a que diga "Engine running" y vuelve a intentar.' }

Paso 'Preparando configuración (.env)'
if (-not (Test-Path .env)) { Copy-Item .env.example .env }
$envTexto = Get-Content .env -Raw
if ($envTexto -notmatch '(?m)^JWT_LLAVE=') {
    # Llave aleatoria de 64 caracteres para firmar los tokens (queda solo en tu .env, fuera de Git).
    $llave = -join ((48..57) + (65..90) + (97..122) | Get-Random -Count 64 | ForEach-Object { [char]$_ })
    Add-Content .env "`nJWT_LLAVE=$llave"
    Write-Host 'Se generó JWT_LLAVE en el archivo .env.'
}

Paso 'Construyendo y levantando contenedores (la primera vez tarda varios minutos)'
docker compose up -d --build
if ($LASTEXITCODE -ne 0) { throw 'docker compose falló. Revisa el mensaje de arriba.' }

Paso 'Esperando a que la API y la base de datos respondan'
$codigo = ''
for ($i = 0; $i -lt 90; $i++) {
    $codigo = & curl.exe -s -o NUL -w '%{http_code}' --max-time 3 http://localhost:8080/health
    if ($codigo -eq '200') { break }
    Start-Sleep -Seconds 2
}

if ($codigo -eq '200') {
    Write-Host "`nListo:" -ForegroundColor Green
    Write-Host '  Aplicación web : http://localhost:8080'
    Write-Host '  Swagger (API)  : http://localhost:5080/swagger'
    Write-Host '  Usuarios demo  : carolina.fuentes@empresa-demo.cl (Admin) y otros; clave Demo.Rrhh2026'
    Start-Process 'http://localhost:8080'
} else {
    Write-Warning 'La API aún no responde. Revisa los registros con:  docker compose logs api'
}

if ($Vigilar) {
    Write-Host "`nModo desarrollo: al guardar cambios en src/ o frontend/ se reconstruye el servicio afectado." -ForegroundColor Cyan
    Write-Host 'Deja esta ventana abierta. Ctrl+C deja de vigilar (los contenedores siguen corriendo).'
    docker compose watch --no-up
} else {
    Write-Host "`nPara detener todo:  docker compose stop"
}
