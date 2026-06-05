Param(
    [string]$CertDir = "docker\traefik\certs"
)

$ErrorActionPreference = 'Stop'

function Write-Note($msg) { Write-Host $msg -ForegroundColor Cyan }
function Write-ErrorExit($msg) { Write-Host $msg -ForegroundColor Red; exit 1 }

if (-not (Get-Command mkcert -ErrorAction SilentlyContinue)) {
    Write-ErrorExit "mkcert is not installed or not in PATH. Install from https://github.com/FiloSottile/mkcert. Example with scoop: `scoop install mkcert` or chocolatey: `choco install mkcert`"
}

Write-Note "Creating certificate directory: $CertDir"
New-Item -ItemType Directory -Path $CertDir -Force | Out-Null

Push-Location $CertDir
try {
    Write-Note "Installing mkcert root CA (may prompt for elevation)..."
    & mkcert -install

    $certFile = Join-Path (Get-Location) "photoapp.localhost.pem"
    $keyFile = Join-Path (Get-Location) "photoapp.localhost-key.pem"

    $hosts = @(
        'photoapp.localhost',
        'localhost',
        'app.localhost',
        'auth.localhost',
        'users.localhost',
        'media.localhost',
        'traefik.localhost',
        '127.0.0.1',
        '::1'
    )

    Write-Note "Generating certificate for: $($hosts -join ', ')"
    & mkcert -cert-file $certFile -key-file $keyFile @hosts

    Write-Note "Certificates created:\n  $certFile\n  $keyFile"
}
finally {
    Pop-Location
}

exit 0
