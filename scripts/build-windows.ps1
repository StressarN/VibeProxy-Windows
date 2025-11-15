param(
    [string]$Configuration = "Release",
    [string]$CliproxyUri
)

$ErrorActionPreference = "Stop"

function Get-CliproxyDownloadUrl {
    param(
        [string]$OverrideUri
    )

    if (-not [string]::IsNullOrWhiteSpace($OverrideUri)) {
        return $OverrideUri
    }

    $api = "https://api.github.com/repos/router-for-me/CLIProxyAPI/releases/latest"
    Write-Host "Resolving latest CLIProxyAPI release from $api"
    $headers = @{ "User-Agent" = "VibeProxy-Windows-Build" }
    $release = Invoke-RestMethod -Uri $api -Headers $headers
    if (-not $release.assets) {
        throw "No assets found in CLIProxyAPI release response"
    }

    $asset = $release.assets |
        Where-Object {
            $_.name -like "*.zip" -and
            ($_.name -match "win" -or $_.name -match "windows") -and
            ($_.name -match "x64" -or $_.name -match "amd64")
        } |
        Sort-Object -Property name |
        Select-Object -First 1

    if (-not $asset) {
        $assetNames = ($release.assets | Select-Object -ExpandProperty name) -join ', '
        throw "Unable to locate Windows x64 asset in CLIProxyAPI release. Available assets: $assetNames"
    }

    return $asset.browser_download_url
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $repoRoot "windows/VibeProxy.Windows.sln"
$resourceDir = Join-Path $repoRoot "windows/src/VibeProxy.Windows/Resources"
$binaryPath = Join-Path $resourceDir "cli-proxy-api.exe"
$outputDir = Join-Path $repoRoot "windows/out"
New-Item -ItemType Directory -Force -Path $outputDir | Out-Null

$cliproxyDownloadUrl = Get-CliproxyDownloadUrl -OverrideUri $CliproxyUri
Write-Host "Using CLIProxyAPI asset: $cliproxyDownloadUrl"

if (-not (Test-Path $binaryPath)) {
    Write-Host "Downloading cli-proxy-api from $cliproxyDownloadUrl"
    $tempZip = New-TemporaryFile
    Invoke-WebRequest -Uri $cliproxyDownloadUrl -OutFile $tempZip.FullName -Headers @{ "User-Agent" = "VibeProxy-Windows-Build" }
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
