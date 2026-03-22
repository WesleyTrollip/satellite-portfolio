param(
    [switch] $Watch
)

Write-Host "Running dotnet test from repository root..." -ForegroundColor Cyan

if ($Watch) {
    dotnet watch test
} else {
    dotnet test
}

