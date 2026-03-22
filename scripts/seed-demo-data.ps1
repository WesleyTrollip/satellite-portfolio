param(
    [string] $ApiBaseUrl = "http://localhost:5014/api"
)

$ErrorActionPreference = "Stop"

function Get-OrCreateInstrument {
    param(
        [string] $Symbol,
        [string] $Name,
        [string] $Sector
    )

    $instruments = Invoke-RestMethod -Uri "$ApiBaseUrl/instruments" -Method Get
    $existing = $instruments | Where-Object { $_.symbol -eq $Symbol } | Select-Object -First 1
    if ($null -ne $existing) {
        if ($existing.id -is [string]) { return $existing.id }
        return $existing.id.value
    }

    $body = @{
        symbol   = $Symbol
        name     = $Name
        sector   = $Sector
        currency = "EUR"
    } | ConvertTo-Json

    $created = Invoke-RestMethod -Uri "$ApiBaseUrl/instruments" -Method Post -ContentType "application/json" -Body $body
    if ($created.id -is [string]) { return $created.id }
    return $created.id.value
}

Write-Host "Seeding demo data against $ApiBaseUrl..." -ForegroundColor Cyan

$msftId = Get-OrCreateInstrument -Symbol "MSFT" -Name "Microsoft" -Sector "Technology"
$novoId = Get-OrCreateInstrument -Symbol "NOVO-B" -Name "Novo Nordisk" -Sector "Healthcare"

$cashEntries = Invoke-RestMethod -Uri "$ApiBaseUrl/cash/entries" -Method Get
$seedCash = $cashEntries | Where-Object { $_.notes -like "*[seed-demo]*" } | Select-Object -First 1
if ($null -eq $seedCash) {
    $cashBody = @{
        type       = 1
        amount     = 10000
        occurredAt = (Get-Date).ToUniversalTime().ToString("o")
        notes      = "[seed-demo] initial deposit"
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$ApiBaseUrl/cash/entries" -Method Post -ContentType "application/json" -Body $cashBody | Out-Null
}

$trades = Invoke-RestMethod -Uri "$ApiBaseUrl/trades" -Method Get
$seedTrades = $trades | Where-Object { $_.notes -like "*[seed-demo]*" }
if (($seedTrades | Measure-Object).Count -eq 0) {
    $trade1 = @{
        instrumentId = $msftId
        side         = 1
        quantity     = 10
        priceAmount  = 320
        feesAmount   = 1
        executedAt   = (Get-Date).AddDays(-5).ToUniversalTime().ToString("o")
        notes        = "[seed-demo] initial MSFT buy"
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$ApiBaseUrl/trades" -Method Post -ContentType "application/json" -Body $trade1 | Out-Null

    $trade2 = @{
        instrumentId = $novoId
        side         = 1
        quantity     = 15
        priceAmount  = 90
        feesAmount   = 1
        executedAt   = (Get-Date).AddDays(-4).ToUniversalTime().ToString("o")
        notes        = "[seed-demo] initial NOVO buy"
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$ApiBaseUrl/trades" -Method Post -ContentType "application/json" -Body $trade2 | Out-Null
}

$today = Get-Date
$price1 = @{
    instrumentId     = $msftId
    date             = $today.ToString("yyyy-MM-dd")
    closePriceAmount = 335
    source           = 1
} | ConvertTo-Json
Invoke-RestMethod -Uri "$ApiBaseUrl/prices/snapshots" -Method Post -ContentType "application/json" -Body $price1 | Out-Null

$price2 = @{
    instrumentId     = $novoId
    date             = $today.ToString("yyyy-MM-dd")
    closePriceAmount = 94
    source           = 1
} | ConvertTo-Json
Invoke-RestMethod -Uri "$ApiBaseUrl/prices/snapshots" -Method Post -ContentType "application/json" -Body $price2 | Out-Null

$rules = Invoke-RestMethod -Uri "$ApiBaseUrl/rules" -Method Get
if (($rules | Measure-Object).Count -eq 0) {
    $rule = @{
        type           = 1
        enabled        = $true
        parametersJson = '{"maxPct":0.35}'
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$ApiBaseUrl/rules" -Method Post -ContentType "application/json" -Body $rule | Out-Null
}

Write-Host "Demo seed complete." -ForegroundColor Green

