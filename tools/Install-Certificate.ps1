<#
.SYNOPSIS
    Makes this computer trust the EasyTax Dashboard package certificate (needed once per computer, before installing).

.DESCRIPTION
    Windows only installs a package signed by a certificate it trusts. This imports the public certificate (.cer) into the
    "Trusted People" store of the computer. It needs administrator rights, so the script asks for them by itself.

        powershell -ExecutionPolicy Bypass -File Install-Certificate.ps1 -Certificate EasyTaxDashboard.cer

    The same can be done by hand: double-click the .cer, "Install Certificate", "Local Machine", "Place all certificates in
    the following store", "Trusted People".
#>
[CmdletBinding()]
param(
    [string]$Certificate = (Join-Path $PSScriptRoot 'EasyTaxDashboard.cer')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Certificate)) {
    throw "Certificate file not found: $Certificate"
}

$certificatePath = (Resolve-Path $Certificate).Path

$isAdministrator = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdministrator) {
    Write-Host 'Administrator rights are needed; asking for them...'
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -Certificate `"$certificatePath`""
    Start-Process powershell -Verb RunAs -ArgumentList $arguments -Wait
    return
}

$imported = Import-Certificate -FilePath $certificatePath -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople'
Write-Host "Trusted: $($imported.Subject) (thumbprint $($imported.Thumbprint))"
Start-Sleep -Seconds 2
