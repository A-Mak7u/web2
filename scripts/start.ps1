Param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Info($msg) { Write-Host "[INFO] $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "[ OK ] $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg)  { Write-Host "[ERR ] $msg" -ForegroundColor Red }

function Test-Command($name) {
    $null = Get-Command $name -ErrorAction SilentlyContinue
    return $?
}

function Wait-HttpOk($Url, $TimeoutSec = 60) {
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $resp = Invoke-WebRequest -Uri $Url -Method GET -UseBasicParsing -TimeoutSec 5
            if ($resp.StatusCode -ge 200 -and $resp.StatusCode -lt 500) { return $true }
        } catch { Start-Sleep -Seconds 1 }
    }
    return $false
}

try {
    if (-not (Test-Command docker)) { throw "Docker not found in PATH" }

    $root = Resolve-Path (Join-Path $PSScriptRoot '..')
    Set-Location $root

    Write-Info "Building & starting containers (docker compose up -d --build)"
    docker compose up -d --build | Out-Null

    $checks = @(
        @{ name='monolith'; url='http://localhost:5000/openapi/v1.json' },
        @{ name='identity'; url='http://localhost:5001/openapi/v1.json' },
        @{ name='catalog' ; url='http://localhost:5002/openapi/v1.json' },
        @{ name='order'   ; url='http://localhost:5003/openapi/v1.json' },
        @{ name='payment' ; url='http://localhost:5004/openapi/v1.json' }
    )

    foreach ($c in $checks) {
        Write-Info "Waiting for service $($c.name): $($c.url)"
        if (-not (Wait-HttpOk -Url $c.url -TimeoutSec 90)) {
            Write-Warn "Service $($c.name) is not ready in time. Continuing, may fail."
        } else {
            Write-Ok "Service $($c.name) is ready."
        }
    }

    # Demo flow: register, login, catalog, order
    Write-Info "Register test user"
    $reg = @{ email = "user@test.com"; password = "P@ssw0rd!" } | ConvertTo-Json
    try { Invoke-RestMethod -Method POST -Uri "http://localhost:5001/api/auth/register" -ContentType "application/json" -Body $reg | Out-Null } catch { }

    Write-Info "Login and get JWT"
    $login = @{ email = "user@test.com"; password = "P@ssw0rd!" } | ConvertTo-Json
    $resp  = Invoke-RestMethod -Method POST -Uri "http://localhost:5001/api/auth/login" -ContentType "application/json" -Body $login
    $token = $resp.accessToken
    Write-Ok   "JWT acquired"
    Write-Host $token

    Write-Info "Fetch products from Catalog"
    $products = Invoke-RestMethod "http://localhost:5002/api/products"
    if (-not $products -or $products.Count -eq 0) { throw "Product list is empty" }
    $prod = $products[0]
    Write-Ok ("Product: {0} ({1}) {2}" -f $prod.name, $prod.id, $prod.price)

    Write-Info "Create order in Order service"
    $orderBody = @{ customerId = [guid]::NewGuid(); items = @(@{ productId = $prod.id; quantity = 1; unitPrice = $prod.price }) } | ConvertTo-Json -Depth 4
    $orderResp = Invoke-RestMethod -Method POST -Uri "http://localhost:5003/api/orders" -ContentType "application/json" -Body $orderBody
    $orderId = $orderResp.id
    Write-Ok ("Order created: {0}, status: {1}" -f $orderId, $orderResp.status)

    Start-Sleep -Seconds 2
    $orders = Invoke-RestMethod "http://localhost:5003/api/orders"
    $latest = $orders | Where-Object { $_.id -eq $orderId }
    if ($latest) { Write-Ok ("Order status after processing: {0}" -f $latest.status) }

    Write-Ok "Done. Logs: docker compose logs -f"
}
catch {
    Write-Err $_
    exit 1
}


