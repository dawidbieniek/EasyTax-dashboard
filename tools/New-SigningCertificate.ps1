<#
.SYNOPSIS
    Creates the self-signed certificate the app package is signed with. Run it once, ever.

.DESCRIPTION
    The package Publisher in Package.appxmanifest has to match the certificate subject exactly, and Windows only accepts
    updates signed by a certificate it trusts, so the same certificate (same subject) has to sign every release. Creating a
    new one later means installing the new public certificate again on every computer.

    The certificate is created in the current user's certificate store and exported to a folder OUTSIDE the repository:

        EasyTaxDashboard.pfx    private key + certificate (signs releases; keep it secret and backed up)
        password.txt            the password of the .pfx (move it into your password manager, then delete the file)
        EasyTaxDashboard.cer    public certificate (safe to share; every computer must trust it, see Install-Certificate.ps1)
        thumbprint.txt          the certificate's thumbprint (Build-Package.ps1 needs it)

    For GitHub releases the .pfx, its password and the thumbprint go into repository secrets (see docs/RELEASING.md).
#>
[CmdletBinding()]
param(
    [string]$Subject = 'CN=dawidbieniek',
    [string]$OutputFolder = (Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'EasyTaxDashboard-signing'),
    [int]$Years = 10
)

$ErrorActionPreference = 'Stop'

$existing = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $Subject -and $_.FriendlyName -eq 'EasyTax Dashboard signing' }
if ($existing) {
    Write-Host "A signing certificate for $Subject already exists (thumbprint $($existing[0].Thumbprint))."
    Write-Host 'Not creating another one: a second certificate would not be trusted by the computers that trust the first.'
    return
}

$certificate = New-SelfSignedCertificate `
    -Type Custom `
    -Subject $Subject `
    -KeyUsage DigitalSignature `
    -FriendlyName 'EasyTax Dashboard signing' `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -NotAfter (Get-Date).AddYears($Years) `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')

New-Item -ItemType Directory -Force $OutputFolder | Out-Null

# 24 random characters, letters and digits only (easy to paste anywhere).
$alphabet = [char[]]'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
$bytes = New-Object byte[] 24
(New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($bytes)
$password = -join ($bytes | ForEach-Object { $alphabet[$_ % $alphabet.Length] })

Export-PfxCertificate -Cert $certificate -FilePath (Join-Path $OutputFolder 'EasyTaxDashboard.pfx') -Password (ConvertTo-SecureString $password -AsPlainText -Force) | Out-Null
Export-Certificate -Cert $certificate -FilePath (Join-Path $OutputFolder 'EasyTaxDashboard.cer') | Out-Null
Set-Content -Path (Join-Path $OutputFolder 'password.txt') -Value $password -Encoding ascii -NoNewline
Set-Content -Path (Join-Path $OutputFolder 'thumbprint.txt') -Value $certificate.Thumbprint -Encoding ascii -NoNewline

Write-Host "Certificate created: $Subject, valid until $($certificate.NotAfter.ToString('yyyy-MM-dd'))"
Write-Host "Thumbprint: $($certificate.Thumbprint)"
Write-Host "Files in $OutputFolder"
Write-Host 'Move password.txt and the .pfx into your password manager / a safe backup, then delete password.txt.'
