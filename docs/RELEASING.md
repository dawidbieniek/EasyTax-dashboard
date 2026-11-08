# Releasing EasyTax Dashboard

The app is installed from a signed MSIX package. Updates come from GitHub Releases through an `.appinstaller` file: Windows
checks it every time the app starts and installs a newer version before the app opens.

## One-time setup

1. **Signing certificate.** `tools/New-SigningCertificate.ps1` creates a self-signed certificate (`CN=dawidbieniek`, valid 10
   years) and writes `EasyTaxDashboard.pfx`, `EasyTaxDashboard.cer`, `password.txt` and `thumbprint.txt` to
   `Documents\EasyTaxDashboard-signing`. Move the `.pfx` and the password into your password manager and delete
   `password.txt`. **Never change the certificate subject, the package name or the publisher** in `Package.appxmanifest`:
   Windows would treat the next build as a different app and updates would stop. A new certificate would have to be trusted
   again on every computer.
2. **GitHub secrets** (repository settings, "Secrets and variables", "Actions"):
   - `SIGNING_CERTIFICATE_BASE64`: the `.pfx` as base64,
   - `SIGNING_CERTIFICATE_PASSWORD`: the contents of `password.txt`.

   With the GitHub CLI:

   ```powershell
   $folder = "$env:USERPROFILE\Documents\EasyTaxDashboard-signing"
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("$folder\EasyTaxDashboard.pfx")) | gh secret set SIGNING_CERTIFICATE_BASE64 --repo dawidbieniek/EasyTax-dashboard
   (Get-Content "$folder\password.txt" -Raw) | gh secret set SIGNING_CERTIFICATE_PASSWORD --repo dawidbieniek/EasyTax-dashboard
   ```
3. **The repository must be public.** Windows downloads the updates from
   `https://github.com/<repository>/releases/latest/download/...` without signing in, which does not work for a private repository.
4. **Every computer trusts the certificate once** (`tools/Install-Certificate.ps1`, see `docs/INSTALL.md`).

## Making a release

1. Raise `<Version>` in `Directory.Build.props` (for example `0.2.0`). The package version is that plus `.0`.
2. Merge to `main`. The *Release* workflow builds the signed package and publishes release `v0.2.0` with four files:
   `EasyTaxDashboard.msix`, `EasyTaxDashboard.appinstaller`, `EasyTaxDashboard.cer`, `Install-Certificate.ps1`.
   If a release for that version already exists the workflow does nothing, so normal pushes to `main` are harmless.
3. Computers pick the update up the next time the app starts.

To build the same files on this computer (output in `artifacts/release`):

```powershell
powershell tools/Build-Package.ps1
```

## Rules that keep updates working

- Versions only go up. A bad release is fixed with a higher version, not by deleting or replacing the old one.
- The database schema only moves forward, which is why the app updates before it opens (`UpdateBlocksActivation`). Until the first
  public release the single `Initial` migration may still be regenerated (see ARCHITECTURE.md, "Migrations"); after that, only
  add incremental migrations.
- The file names of the release assets must not change; the `.appinstaller` links to them by name.

## Icon

The icon is a placeholder (white "ET" on blue). To use a real one, run
`powershell tools/New-Icons.ps1 -Source path\to\icon.png` (a square PNG, 1024 x 1024 is best) and commit the files it writes to
`src/EasyTax.App/Assets`.
