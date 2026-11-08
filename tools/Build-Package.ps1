<#
.SYNOPSIS
    Builds the signed MSIX package and the files of a release.

.DESCRIPTION
    1. Takes the version from <Version> in Directory.Build.props and writes it into Package.appxmanifest.
    2. Publishes the app as a self-contained, signed MSIX.
    3. Writes the release files to the output folder (default artifacts/release):

        EasyTaxDashboard.msix            the package
        EasyTaxDashboard.appinstaller    tells Windows where the newest version is (updates)
        EasyTaxDashboard.cer             the public certificate (computers must trust it once)
        Install-Certificate.ps1          helper that trusts the certificate

    The files are named without a version so that GitHub's "latest release" links keep working:
    https://github.com/<repository>/releases/latest/download/<file>

    The certificate is found by its thumbprint in the current user's certificate store (created by New-SigningCertificate.ps1,
    or imported from the .pfx on a build server).

.EXAMPLE
    powershell tools/Build-Package.ps1
    powershell tools/Build-Package.ps1 -Thumbprint 825C532F7A65F1A88DE7F559B51247FA87640882
#>
[CmdletBinding()]
param(
    # Defaults: $env:EASYTAX_SIGNING_THUMBPRINT, else the thumbprint.txt written by New-SigningCertificate.ps1.
    [string]$Thumbprint,
    [string]$OutputFolder,
    # GitHub repository the releases live in (owner/name); only used for the update links in the .appinstaller.
    [string]$Repository = 'dawidbieniek/EasyTax-dashboard'
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if (-not $OutputFolder) { $OutputFolder = Join-Path $root 'artifacts/release' }

if (-not $Thumbprint) { $Thumbprint = $env:EASYTAX_SIGNING_THUMBPRINT }
if (-not $Thumbprint) {
    $file = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'EasyTaxDashboard-signing/thumbprint.txt'
    if (Test-Path $file) { $Thumbprint = (Get-Content $file -Raw).Trim() }
}
if (-not $Thumbprint) { throw 'No signing certificate thumbprint. Pass -Thumbprint, or run tools/New-SigningCertificate.ps1 first.' }

$certificate = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Thumbprint -eq $Thumbprint }
if (-not $certificate) { throw "Certificate $Thumbprint is not in the current user's store (Cert:\CurrentUser\My)." }

# --- version -------------------------------------------------------------------------------------------------------------
[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
$version = [string]($props.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw "Version '$version' in Directory.Build.props must look like 1.2.3." }
$packageVersion = "$version.0"

$manifestPath = Join-Path $root 'src/EasyTax.App/Package.appxmanifest'
[xml]$manifest = Get-Content $manifestPath
$identity = $manifest.Package.Identity

if ($identity.Publisher -ne $certificate.Subject) {
    throw "Manifest Publisher '$($identity.Publisher)' does not match the certificate subject '$($certificate.Subject)'. The two must be identical."
}

if ($identity.Version -ne $packageVersion) {
    $identity.Version = $packageVersion
    $manifest.Save($manifestPath)
    Write-Host "Package.appxmanifest version set to $packageVersion"
}

# --- build ---------------------------------------------------------------------------------------------------------------
$packageFolder = Join-Path $root 'artifacts/package'
if (Test-Path $packageFolder) { Remove-Item $packageFolder -Recurse -Force }

Write-Host "Building EasyTax Dashboard $packageVersion (signed with $($certificate.Subject))..."
dotnet publish (Join-Path $root 'src/EasyTax.App/EasyTax.App.csproj') -c Release `
    -p:Packaged=true `
    -p:PackageCertificateThumbprint=$Thumbprint `
    -p:AppxPackageDir="$packageFolder\"
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)." }

$msix = Get-ChildItem $packageFolder -Recurse -Filter '*.msix' | Select-Object -First 1
if (-not $msix) { throw "No .msix was produced in $packageFolder." }

# --- release files -------------------------------------------------------------------------------------------------------
if (Test-Path $OutputFolder) { Remove-Item $OutputFolder -Recurse -Force }
New-Item -ItemType Directory -Force $OutputFolder | Out-Null

Copy-Item $msix.FullName (Join-Path $OutputFolder 'EasyTaxDashboard.msix')
Export-Certificate -Cert $certificate -FilePath (Join-Path $OutputFolder 'EasyTaxDashboard.cer') | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'Install-Certificate.ps1') $OutputFolder

$base = "https://github.com/$Repository/releases/latest/download"
$appInstaller = @"
<?xml version="1.0" encoding="utf-8"?>
<AppInstaller xmlns="http://schemas.microsoft.com/appx/appinstaller/2018" Version="$packageVersion" Uri="$base/EasyTaxDashboard.appinstaller">
  <MainPackage Name="$($identity.Name)" Publisher="$($identity.Publisher)" Version="$packageVersion" ProcessorArchitecture="x64" Uri="$base/EasyTaxDashboard.msix" />
  <UpdateSettings>
    <!-- Checks at every start and installs a newer version before the app opens: everyone runs the same version, which matters
         because the database schema only moves forward. Offline, the app simply starts. -->
    <OnLaunch HoursBetweenUpdateChecks="0" ShowPrompt="true" UpdateBlocksActivation="true" />
    <AutomaticBackgroundTask />
  </UpdateSettings>
</AppInstaller>
"@
Set-Content -Path (Join-Path $OutputFolder 'EasyTaxDashboard.appinstaller') -Value $appInstaller -Encoding utf8

Write-Host ''
Write-Host "Done. Release files for version ${packageVersion}:"
Get-ChildItem $OutputFolder | ForEach-Object { '  {0,-34} {1,10:N1} MB' -f $_.Name, ($_.Length / 1MB) }
