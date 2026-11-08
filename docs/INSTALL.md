# Installing EasyTax Dashboard

Needs Windows 10 (version 2004 or newer) or Windows 11, 64-bit. No other software has to be installed.

All files are on the latest release: <https://github.com/dawidbieniek/EasyTax-dashboard/releases/latest>

## First installation (once per computer)

1. **Trust the certificate.** The app is signed with a certificate made for it, which Windows does not know yet. Download
   `EasyTaxDashboard.cer` and `Install-Certificate.ps1` into the same folder, right-click `Install-Certificate.ps1`, choose
   "Run with PowerShell" and confirm the administrator prompt.

   Without the script: double-click `EasyTaxDashboard.cer`, "Install Certificate", "Local Machine", "Place all certificates in
   the following store", "Browse", "Trusted People".
2. **Install the app.** Download `EasyTaxDashboard.appinstaller` and open it (double-click), then "Install".

When the app starts for the first time it shows a **recovery key**. Write it down or print it and keep it safe: it is the only way
to get the data back after a computer failure or a reinstall.

## Updates

Nothing to do. Every time the app starts, Windows checks for a newer version and downloads it in the background. It cannot replace the
app while it is running, so the new version takes effect the **next time the app is started**: close it and open it again after a
release. Without internet the app simply starts.

## Backups and uninstalling

Backups go to `Documents\EasyTax\Backups` (the folder can be changed in Settings). They are encrypted; opening one on another
computer needs the recovery key. The app keeps its data inside its own package, so **uninstalling the app deletes it**. The backups in Documents stay. Check that a
recent backup exists (Settings shows the last one) and that you have the recovery key before uninstalling.
