param(
    [string]$Configuration = "Release",
    [string]$CliproxyUri = "https://github.com/router-for-me/CLIProxyAPI/releases/latest/download/cli-proxy-api-win-x64.zip"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $repoRoot "windows/VibeProxy.Windows.sln"
$resourceDir = Join-Path $repoRoot "windows/src/VibeProxy.Windows/Resources"
$binaryPath = Join-Path $resourceDir "cli-proxy-api.exe"
$outputDir = Join-Path $repoRoot "windows/out"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

if (-not (Test-Path $binaryPath)) {
    Write-Host "Downloading cli-proxy-api from $CliproxyUri"
    $tempZip = New-TemporaryFile
    Invoke-WebRequest -Uri $CliproxyUri -OutFile $tempZip.FullName
    $tempExtract = New-Item -ItemType Directory -Path ([System.IO.Path]::GetTempPath()) -Name ([System.Guid]::NewGuid())
    Expand-Archive -Path $tempZip.FullName -DestinationPath $tempExtract.FullName -Force
    $downloadedBinary = Get-ChildItem -Path $tempExtract.FullName -Recurse -Filter "cli-proxy-api*.exe" | Select-Object -First 1
    if (-not $downloadedBinary) {
        throw "cli-proxy-api executable not found in archive"
    }
    Copy-Item $downloadedBinary.FullName $binaryPath -Force
    Remove-Item $tempZip.FullName -Force
    Remove-Item $tempExtract.FullName -Force -Recurse
}

dotnet restore $solution
dotnet test $solution -c $Configuration

$publishDir = Join-Path $outputDir "publish"
dotnet publish (Join-Path $repoRoot "windows/src/VibeProxy.Windows/VibeProxy.Windows.csproj") `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -o $publishDir

$zipPath = Join-Path $outputDir "VibeProxy-Windows-$Configuration.zip"
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $zipPath
Write-Host "Artifacts ready at $zipPath"
