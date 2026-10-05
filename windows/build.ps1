$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$project = Join-Path $PSScriptRoot 'MenuBarStats.Windows/MenuBarStats.Windows.csproj'
$tests = Join-Path $PSScriptRoot 'MenuBarStats.Tests/MenuBarStats.Tests.csproj'
$output = Join-Path $PSScriptRoot 'artifacts/win-x64'
dotnet test $tests -c Release
if ($LASTEXITCODE -ne 0) { throw 'Testes falharam' }
dotnet publish $project -c Release -r win-x64 --self-contained true -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publicação falhou' }
Copy-Item (Join-Path $PSScriptRoot 'README.md') $output
Copy-Item (Join-Path $PSScriptRoot '../LICENSE') $output
Write-Host "Pacote portátil: $output/MenuBarStats.exe"
