Param(
    [switch]$Start
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "[ OK ] $msg" -ForegroundColor Green }
function Write-Err($msg)  { Write-Host "[ERR ] $msg" -ForegroundColor Red }

try {
    Set-Location (Resolve-Path (Join-Path $PSScriptRoot '..'))

    if ($Start) {
        Write-Info "Ensuring services are up (docker compose up -d)"
        docker compose up -d monolith identity catalog order payment | Out-Null
    }

    $urls = @(
        'http://localhost:5000/swagger',
        'http://localhost:5001/swagger',
        'http://localhost:5002/swagger',
        'http://localhost:5003/swagger',
        'http://localhost:5004/swagger'
    )

    foreach ($u in $urls) { Start-Process $u }
    Write-Ok "Swagger UI tabs opened."
}
catch {
    Write-Err $_
    exit 1
}




