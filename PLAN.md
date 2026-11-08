# EasyTax — plan & progress

Fresh WinUI 3 rewrite of the old TaxDashboard (inspiration only; no backward compatibility).
Reference UI: `docs/wireframes.html` (dark). Older drafts: `docs/archive/`. Structure: `ARCHITECTURE.md`.

Legend: `[ ]` todo · `[~]` in progress · `[x]` done · **(?)** needs a decision

## Decisions
- **.NET 10**, Windows only (`net10.0-windows10.0.19041.0`), WinUI 3 (Windows App SDK 2.6), unpackaged for now.
- MVVM with CommunityToolkit.Mvvm; DI + logging via Generic Host.
- Tabs, CRLF, C# conventions enforced by `.editorconfig` / `.gitattributes`; warnings are errors. No manual formatting passes.
- **No unit tests** for now. **No heavy architecture** (no DDD / vertical-slice frameworks): code is organised by feature folders as needed.
- Core = the domain layer: EF entities are the rich domain objects (no separate persistence models). Ids are **Guid v7** assigned by the domain.
- DI registration via `ServiceCollectionExtensions` using C# 14 extension blocks.
- Data: EF Core + SQLite, encrypted with SQLCipher: one random key per install, shown once to the user as a recovery key and wrapped with Windows DPAPI for daily use. Built (E.1–E.5); backups (E.6) and the notices file (E.7) are still open.
- Charts/gauges: LiveChartsCore.SkiaSharpView.WinUI (spike verified). Added with the dashboard milestone.
- Grid: WinUI.TableView candidate; verify in the clients-list task.
- UI language: **Polish**, all strings in `.resw` resources (English can be added later).
- Photos: optional, stored in the DB (separate table), shown with `PersonPicture`.
- Years are **user-managed**: seeded 2026–2030, user can add years and remove a year only if it has no data.
- Settings page: theme (System / Light / **Dark default**) and app version shown as plain text. No DB settings.
- **Accent colour = the Windows system accent** (WinUI's default), so the app follows the user's Windows personalisation. No colour setting, no custom palette.
- Out of scope for v1: backups, notifications, email, tax calculator, list filters.

## Data model (draft)
One `MonthRecord` per client per month replaces the old app's five child tables.

- All entities derive from `Entity` (`Guid Id`, v7). `AccountingYear` is keyed by its year number instead.
- `Client`: FirstName, LastName, Gender, Nip, Phone, Email, JoinDate, BankId, SuspendedAt (nullable timestamp, null = active; `IsSuspended` becomes derived; set on suspend, cleared on unsuspend; see "Privacy and data retention"), Notes, PlanName, AuthorizationFee, SubscriptionFee, EmploymentContract, CashMethod, TaxType, PitPaymentFrequency, IsVatPayer, VatPaymentFrequency, UsesCashRegister, FirstCashRegisterDate, ReliefType, ReliefSinceDate (dates are `DateOnly`; enums are stored as text)
  - **per-client checklist items** (same rules as the old app; stored as done-at timestamps, null = not done): `VatRDoneAt`, `Ceidg1DoneAt`, `ZusDoneAt`, `InvoicedAt`, `ZusDraDoneAt`, `TaxDoneAt`
- `ClientPhoto`: ClientId (PK/FK), Bytes, ContentType
- `Bank`: Id, Name (seeded)
- `AccountingYear`: Year (PK), seeded 2026–2030
- `MonthRecord` (unique on ClientId + Month): Month (first day), Income, SalesInvoices, CostInvoices, IsAccounted
  - **per-month checklist items** (same rules as the old app; done-at timestamps): `JpkV7DoneAt`, `VatUeDoneAt`
- Calculated, not stored: relief end date, income sums per VAT / cash-register / ZUS year, limits.

## Open questions
None at the moment.

Resolved: checklist = old app's rules (VAT-R, CEIDG-1, ZUS, Invoiced, ZUS DRA, Tax are per client; JPK V7 and VAT UE are per month). Accent colour = Windows system accent.

## Milestones & tasks

### M0 — Wireframes
- [x] Charts feasibility spike
- [x] Reference wireframes approved

### M1 — Foundation (skeleton)
- [x] 1.1 Solution: `EasyTax.Core`, `EasyTax.Data`, `EasyTax.App`; `.editorconfig`, `.gitattributes`, `global.json`, shared props, central package versions
- [x] 1.2 App boots: Generic Host, DI, shell window with title bar + icon-rail navigation, placeholder pages (Dashboard, Clients, Settings)
- [x] 1.3 Navigation service + page registry + view-model lifecycle
- [x] 1.4 Settings service (JSON) + theme service (System/Light/Dark, default Dark) + Settings page with theme selector and version text
- [x] 1.5 Localization via `.resw` (pl-PL) for XAML (`x:Uid`) and code (`ILocalizer`)
- [x] 1.6 Data layer wired end to end: `DbContext`, repository, initializer; Clients page reads a count from the DB
- [x] 1.7 Reorganised into feature folders (Clients, Dashboard, Settings, Shell…), Guid v7 ids, extension-block DI, `Resources/` folder, code-style rules enforced in the build (unit tests dropped)
- [x] 1.8 Architecture approved; `EnsureCreated` replaced by migrations (task 2.0)
- [x] 1.9 File logging: Serilog, one file per day in `%LocalAppData%\EasyTax\logs`, 14 days kept; Entity Framework limited to warnings

### M2 — Domain model & core logic (pure, no UI)
All of M2 is done and was checked with a throwaway console program (not part of the repo; no unit tests by decision): 56 checks, including the old app's relief example (join 14.01.2025 → ends 31.07.2025).

- [x] 2.0 Full entities (`Client`, `ClientPhoto`, `Bank`, `MonthRecord`, `AccountingYear`) + EF configurations + `Initial` migration; banks (27) seeded idempotently, years 2026–2030 seeded once when the database is created
- [x] 2.1 Relief end date on `Client.ReliefEndDate` (Ulga na start / ZUS preferencyjny / Mały ZUS Plus, ported from the old app)
- [x] 2.2 + 2.3 Income sums and limits in `LimitCalculator` (VAT 200 000 prorated in the join year, cash register 20 000, ZUS Plus 120 000); `LimitStatus` gives income, limit, ratio and state (Ok / Warning ≥ 90% / Over); not-applicable cases handled
- [x] 2.4 Month rules on `Client` (`IsMonthSelectable`, `GetMonthState` → BeforeJoin / NotAccounted / Accounted); `MonthRecord.Month` is always normalised to the first day; one record per client and month enforced by a unique index
- [x] 2.5 Validation in `ClientValidation`: NIP (10 digits + checksum, normalisation), phone, email (phone/email optional)
- [x] 2.6 Year rules in `IAccountingYearRepository`: add (no duplicates), remove only when the year has no month data
- Repositories done: clients (CRUD), banks (list), accounting years. Month-record and photo repositories come with the dashboard and client-page milestones.

### M3 — Shell
All of M3 is done and checked by running the app against demo data (clicking through every control and restarting to confirm the selection is restored).

- [x] 3.1 App bar with avatar (`PersonPicture`) + client picker (search box that opens the full list on focus, skips suspended clients) and prev/next; shared `ClientContext` service holds the selected client, year and month and persists them to `settings.json`. The bar shows per page via `ViewModelBase.AppBarMode` (Hidden / ClientOnly / Full); the dashboard uses Full, the client page will use ClientOnly.
- [x] 3.2 Year picker driven by the user-managed years + month strip: green = accounted, orange = not accounted, grey and disabled = before the client joined; the selected month is underlined. Selecting a client who joined later snaps the selection to their join month.
- [x] 3.3 "Rozliczony / Nierozliczony" toggle for the selected month (green when accounted); creates the month record on first use. `IMonthRecordRepository` added (read a client's year, upsert).
- [x] 3.4 Empty state on the dashboard (message + button to the clients page); the app bar shows a disabled "Brak klientów" box.
- Not done on purpose: the narrow-window fallback for the month strip (it scrolls horizontally for now); revisit in the dashboard layout task 5.6.

### M4 — Clients
All of M4 is done and checked in the running app (list, menu, add with validation, edit with validation, unsaved-changes dialog in all three branches, delete with confirmation, photo pick/show, narrow-window layout).

- [x] 4.1 Clients list on **WinUI.TableView** (spike passed: sorting by header, row events, context flyout, templated cells with avatars all work). Search box filters name, NIP, bank, phone, email; suspended rows are dimmed with a "zawieszony" tag; the last column shows the accounted status of the month selected in the app bar. **A single tap on a row opens the client** (the grid does not raise the standard item-click/double-tap events reliably, so the tap is handled on the grid and mapped to its row). Built-in column filtering is available but switched off (`CanFilterColumns="False"`) until filters are designed.
- [x] 4.2 Right-click menu: Open, Suspend / Unsuspend, Delete… (confirmation dialog with the client's name on its own bold line, default button is Cancel; deletes month records and photo via cascade). List avatars show the client's photo when there is one (all photos are loaded with the list; fine for hundreds of 256 px JPEGs).
- [x] 4.3 Add-client dialog: required fields validated on submit (format errors show while typing), optional photo, avatar shows initials live while typing
- [x] 4.4 Client page header band: photo with Change / Remove, name + status badge, contact line, chips (tax, PIT, VAT, cash register, relief with end date, plan), stats (client since, bank, status of the selected month), Edit / Delete
- [x] 4.5 Tabs (Dane / Podatki i ulgi / Abonament) in view mode and edit mode with inline validation; the relief end date is calculated live from the form; Save / Cancel appear in the **header band** (not in the app bar as the wireframe had: the app bar has no slot for page actions yet); the app bar's client picker is locked while editing; leaving the page with unsaved changes asks Save / Discard / Cancel
- [x] 4.6 Photo: pick (file picker) → crop to a centred square → shrink to ≤ 256 px → JPEG → stored in `ClientPhoto`; shown in the header, the app bar and the add dialog. Limitation: EXIF rotation is ignored, so a phone photo taken sideways can appear rotated
- [x] 4.7 Responsive layout: content column grows with the window up to 960 px, left-aligned; below ~1000 px the card grids collapse to one column (`AdaptiveTrigger`)
- Known cosmetic limits, left for polish (M7): the header chips do not wrap, so in windows narrower than ~1000 px the last chips are clipped.

### M5 — Dashboard
All of M5 is done and checked in the running app (edit, restart to confirm it was saved, several clients, narrow window, light theme).

- [x] 5.1 Month card: income and sales/cost invoice counts for the selected month. A value is saved when it is committed (Enter or leaving the box), not on a timer, so it can never land on a different month if you switch quickly. Clearing a box saves 0: the month record also holds the accounted flag and the checklist, so it is not deleted (the old app removed the record at 0). Disabled for months before the client joined.
- [x] 5.2 Checklist card: per-client items (VAT-R, CEIDG-1, ZUS, Zafakturowany, ZUS DRA, Podatek) and per-month items (JPK V7, VAT UE). Clicking toggles an item; done items turn green and show when they were done.
- [x] 5.3 Notes card: a rich text editor (bold, italic, underline, bullet list; Ctrl+B/I/U work too) that autosaves 0.8 s after you stop typing. The pending save carries the client's id, so switching client right away still saves to the right one. Stored as RTF text in the existing `Client.Notes` column (no schema change; RTF is plain ASCII text). Old plain-text notes still load and become RTF the next time they are edited. Trade-off: the stored value is not human-readable in a database tool.
- Polish pass on the dashboard (after first review): checklist has a divider instead of labels; invoice counts have ± spin buttons; gauge tooltips are on the text only; gauge cards have equal height; both ends of the gauge arc are rounded; refreshes are granular (gauge arcs animate their value, checklist/month-strip/chart values change only when they differ, typing in notes no longer refreshes every screen) which removed the flicker. Measured by sampling screenshots after clicks: no changed pixels in the gauges, charts or untouched months.
- [x] 5.4 Gauges for VAT, cash register and ZUS Plus: amount, limit, percentage, amber from 90% ("Blisko limitu") and red from 100% ("Limit przekroczony"); tooltip says which dates are counted; "not applicable" text when the client has no cash register or is not on Mały ZUS Plus. VAT limit is prorated in the join year (checked: 162 739,73 zł for a 10.03.2026 join).
- [x] 5.5 Income line chart and sales/cost bar chart for the selected year (LiveCharts2), updating live as inputs change. Chart colours come from `ChartPalette`: the Windows accent colour (the lighter shade on dark, darker on light) and the current theme.
- [x] 5.6 Layout: three gauge cards on top, the two charts stacked on the left, month / checklist / notes cards on the right; below ~1100 px the right column moves under the charts. The page scrolls when the window is short.
- Polish done: minimum window width 1200 px keeps the month strip whole; charts show "Brak danych za ten rok." when the year has no data.

### M6 — Settings
- [x] 6.1 Years management UI in Settings: list of years (a year with data shows "Zawiera dane" and cannot be removed), add a year 2000–2100. Theme and version were done earlier.

### M7 — Polish & release
- [x] 7.1 Keyboard shortcuts, one `Shortcuts` helper; keys listed in ARCHITECTURE.md. Tooltips show "name (key)" on buttons.
- [x] 7.3 Error handling: toasts (in-app `InfoBar`s, `IToastService`), friendly Polish messages for database / disk errors, startup failure window, error bar with retry on the clients list, success toasts for save / add / delete / suspend / years
- [~] 7.4 Packaging and updates. **Built, not yet tried end to end** (see the status bullet). Original decision follows. **Decided: packaged MSIX with an `.appinstaller`** (on a fileshare or web URL) for Windows' built-in "check remote version and update", **signed with a self-signed certificate**. Do it before the first real release, because packaged apps get redirected, private app data and switching later moves the data folder and the wrapped encryption key.
  - **Status:** done: `Package.appxmanifest` (name `dawidbieniek.EasyTaxDashboard`, display name "EasyTax Dashboard", publisher `CN=dawidbieniek`), placeholder icons (`tools/New-Icons.ps1`), self-contained signed package built by `tools/Build-Package.ps1` (about 104 MB, no prerequisites on the target PC), `.appinstaller` with links to GitHub's `releases/latest/download/...`, certificate (valid to 2036; `tools/New-SigningCertificate.ps1`, trust helper `tools/Install-Certificate.ps1`), release workflow `.github/workflows/release.yml` (a new `<Version>` on `main` makes a release, like the old TaxDashboard), docs `docs/RELEASING.md` and `docs/INSTALL.md`.
  - **Tried on a real install** (local `.msix`, certificate trusted): installs as a normal signed app, starts from the app list, first-start recovery key window, dashboard, Settings, backup (automatic one landed in `Documents\EasyTax\Backups`), restore dialogs including the "other install's key" window. The packaged app keeps its data in the package folder (`AppPaths` uses `ApplicationData.Current.LocalFolder`), which is separate from a development build's `%LocalAppData%\EasyTax`. **Uninstalling removes that data and leaves the backups in Documents** (checked).
  - **Not yet tried:** the restart after a restore when installed (`AppInstance.Restart`), a real update through the `.appinstaller` (needs a published release, then a second one: 0.1.0, then 0.1.1), and the GitHub workflow itself. **Still to do by hand:** add the two GitHub secrets and make sure the repository is public.
  - Create the certificate once (subject = the manifest `Publisher`); keep the `.pfx` in the admin's password manager; install the public `.cer` into "Trusted People" on each machine (one-time, admin).
  - Keep the same certificate subject and package identity for the life of the app. A renewed or recreated certificate with the same subject still counts as an update, but must be trusted again on every machine.
  - If the app is ever installed on machines the admin does not control, move to Trusted Signing or a commercial certificate first (the subject text must stay the same).
  - Test an update with a real encrypted DB before shipping. Uninstall deletes the data folder, so the uninstall docs must point at backups and the recovery key.
- [x] 7.5 Demo data for development: `EasyTax.exe --seed-demo` (Debug builds only) fills an empty database with five sample clients and 2026 month records. `EASYTAX_DATA_DIR` redirects the data folder (database and settings), so testing never touches real data.

## Backups (built)
Settled with the user. Everything below is built, including re-applying the deletion log on restore.

**Prerequisite: single instance**
- [x] B.0 Single instance: `AppInstance.FindOrRegisterForKey("EasyTax.MainInstance")` at the top of `OnLaunched`; a second launch redirects to the first (which comes to the front, restored if minimized) and exits. After a restore the old process starts the new one with `--restart-after <pid>` and the new one waits for it to end first (`WaitForPreviousInstance`), so it is not mistaken for a second copy. The log sink is shared so a short-lived second launch can still write.

**Settings** (new card on the Settings page)
- `BackupFolder` (default `Documents\EasyTax\Backups`, user-configurable, a fileshare path must work as plain file copy) and a scheduled-backups on/off toggle. Interval fixed at 7 days. No retention/pruning: keep all.

**Backup flow** (`Backups/` feature folder, `BackupService`, plain methods with guard clauses)
- [x] B.1 `IBackupService.RunAsync` (`EasyTax.Data/Backups/BackupService`): SQLite online backup into a temp file, both ends keyed; integrity check on the copy; zip (`easytax.db` stored without compression, `manifest.json`) written as `.zip.tmp` and renamed last. Checked: the zip holds an encrypted database (random header) and a readable manifest.
- [x] B.2 `DeleteStaleTempFiles` at startup removes `.zip.tmp` leftovers; they never count as backups (`GetLatestBackup` only accepts `EasyTax-yyyy-MM-dd-HHmm.zip`, so the `prerestore` safety copies are ignored too).
- [x] B.3 `BackupManager.RunScheduledIfDueAsync` runs after the main window opens: if automatic backups are on and the newest valid backup is older than 7 days (or missing) it backs up in the background. Startup only, no timer. Checked: the first start with an empty folder made a backup by itself.
- [x] B.4 Success toast "Utworzono kopię zapasową."; failure toast with the friendly reason (disk, busy database, ...); details in the log.

**Restore**
- [x] B.5 `RestoreCoordinator`: file picker, manifest check ("not a backup" and "newer version" are refused with a toast), confirmation dialog naming the backup date, extract to temp, open with the local key or ask for the recovery key (`KeyRecoveryWindow`, restore wording), migrate an older schema, safety backup (`EasyTax-prerestore-...zip`, aborts the restore if it fails), swap the file, store the key that worked, restart. Checked end to end: data rolled back and the app restarted; a backup from another install asked for its key, then opened after the restart with the stored key. The deletion log step (P.3) is part of the restore.

- [x] B.6 Settings card "Kopie zapasowe": folder (read-only box + Wybierz), automatic-backup checkbox, last backup time, "Utwórz kopię teraz", "Przywróć z pliku...". Buttons are disabled while a backup or restore runs.

Out of scope: retention, off-machine copies, Windows scheduled tasks. Encryption has its own section below; once it lands, backups are encrypted copies of the encrypted DB (B.1 opens both ends of `BackupDatabase` with the key; B.5 needs the key to run its integrity check, and asks for the recovery key when the local one cannot open the file).

## Database encryption (planned, before the first release)
Settled with the user. Goal: a stolen `.db` file or backup `.zip` is useless without the app's key. Not protected against: someone using the user's logged-in Windows account (they already have the app). No password prompt at launch.

- **Engine:** SQLCipher (swap the SQLite native bundle, e.g. `SQLitePCLRaw.bundle_e_sqlcipher`; the password is set on the connection string). EF Core and the migrations keep working.
- **One key per install**, generated randomly on first run. The same key encrypts the DB and every backup made from it, so one recovery key opens any backup.
- **Daily use:** the key is wrapped with Windows DPAPI (`ProtectedData`, scope CurrentUser) and stored in the app data folder next to the DB. Unwrapped silently on start; no prompt.
- **Recovery key:** the same key is shown to the user once, as a grouped code (e.g. `A1B2-C3D4-...`), so it survives a dead machine or a lost Windows profile. If the user loses both the machine and the recovery key, the data is gone. This is by design and the setup dialog says so plainly.
- **Not chosen:** hardcoded key (extractable from the binary, one leak exposes every install), key fetched from a server (needs a service plus per-install enrollment; too heavy), admin-typed key at setup (fallback if the user flow proves too awkward).

**Tasks**
- [x] E.0 Spike **passed**, done on a throwaway copy of the repo (the real code is untouched). Findings:
  - **Packages:** in `EasyTax.Data` replace `Microsoft.EntityFrameworkCore.Sqlite` with `Microsoft.EntityFrameworkCore.Sqlite.Core` (10.0.14) + `SQLitePCLRaw.bundle_e_sqlcipher` (2.1.11). Call `SQLitePCL.Batteries_V2.Init()` once before the first connection (call it from `EasyTaxDbContextFactory` too, to be safe). The password goes on the connection string (`SqliteConnectionStringBuilder.Password`). `e_sqlcipher.dll` is copied next to `EasyTax.exe` automatically.
  - **Works with** .NET 10, unpackaged WinUI 3 (app starts, dashboard and charts load), EF Core 10: the existing `Initial` migration applied and the demo seed ran on an encrypted file. `dotnet ef migrations list` still works.
  - **Really encrypted:** the `.db` header is random bytes (no "SQLite format 3"), and so is the `-wal` file.
  - **Failure cases** all end in the startup error window, no crash: wrong key, no key on an encrypted DB, plain DB opened with a key (this is also E.5's rejection of plain databases). The right key opens normally.
  - **Licence:** the NuGet wrappers are Apache-2.0; the native library is SQLCipher Community Edition, BSD-3-Clause (Zetetic), free for commercial use but the copyright notice and licence text must ship with the app (add a third-party notices file to the package, task E.7). The builds are SourceGear's "unofficial and unsupported"; Zetetic sells official builds if that ever matters.
  - **Not covered by the spike:** the packaged MSIX (check that `e_sqlcipher.dll` is inside the package when 7.4 is done) and the cost of an `ALTER`-style migration on a large encrypted DB (not a concern at this data size).
- [x] E.7 Third-party notices: `THIRD-PARTY-NOTICES.txt` at the repo root, generated by `tools/Update-ThirdPartyNotices.ps1` from the packages that really ship (65 now) plus .NET and SQLCipher, with the MIT / Apache-2.0 texts, the Microsoft licence files and the Zetetic BSD text. Copied next to `EasyTax.exe`; Settings has a "Licencje składników zewnętrznych" link that shows it in a dialog. Regenerate it (build first, then run the script) whenever packages change. **The MSIX (7.4) must include this file.**
- [x] E.1 `DatabaseKeyService` (`Security/` in the app): 32-character Crockford base32 key (160 bits), wrapped with DPAPI (CurrentUser, app-specific entropy) into `easytax.key` next to the database, written via a temporary file. `EasyTaxDataOptions.Password` is set once before the database is touched; opening without it throws, so an unencrypted file is never created. Rule kept: a new key is only created when no database file exists (`DatabaseKeyStartup`).
- [x] E.2 First-run window (`KeySetupWindow`): key in groups of four, Copy / Save to file / Print (the text file goes to the default printer through the shell "print" verb), and the user must retype the first and last group before Continue. The key is stored only after that; closing the window leaves nothing behind. Checked in the running app.
- [x] E.3 Settings card "Szyfrowanie danych" with "Pokaż klucz odzyskiwania": a dialog with the key and a Copy button (toast on copy).
- [x] E.4 Recovery (`KeyRecoveryWindow`): when the stored key is missing, unreadable or does not open the database, the user types the recovery key; it is checked by actually opening the database (`IDatabaseInitializer.CanOpenAsync`, own unpooled connection) before it is re-wrapped for this machine. Checked: missing key file, wrong key (message), right key.
- [x] E.5 A database with the plain SQLite header is refused at startup with its own message (`PlainDatabaseException`), without touching the file. Old development databases must be moved or deleted.
- [x] E.6 Backup tasks B.1 and B.5 rewritten for the encrypted database (both ends keyed, restore asks for the recovery key when the local one does not open the file and keeps the key that worked). The backup feature itself is still unbuilt.

Open decisions (defaults assumed unless changed): the key format is a grouped code; the recovery-key step in first-run setup is mandatory.

## Privacy and data retention (built)
Not legal advice; confirm the final policy with whoever handles GDPR for the practice. Background: erasure (Art. 17) does not apply to data that must be kept for a legal obligation (as understood, accounting/tax records are kept about 5 years in Poland), and data in backups is acceptable only if it stays unused and eventually expires.

**Decisions**
- **No backup retention policy for now.** Backups are kept forever (see Backups). Known gap: a deleted client's data lives on in old backups with no expiry. Accepted for now; revisit before real client data, with a configurable retention period and a prune step.
- **Suspension timestamp (built):** `Client.SuspendedAt` (replaces the `IsSuspended` flag, which becomes derived). It records when service ended, so it is easy to tell later whether the retention period has passed. It is an aid, not the legal start of the period (that depends on the tax year of the records). Done (folded into the single `Initial` migration, see ARCHITECTURE "Migrations"); suspending sets `DateTime.UtcNow`, reactivating clears it; the client page shows "Zawieszony od dd.MM.yyyy"; `IsSuspended` is derived.
- **Deletion log** so a restored backup cannot bring deleted clients back:
  - [x] P.1 `DeletedClient` table (id + UTC time, no personal data; migration `AddDeletedClients`). `IClientRepository.DeleteAsync` removes the client, month records and photo and writes the record in one transaction (tested: a failing record insert rolls the whole delete back). Returns the record, or null for an unknown id (nothing recorded).
  - [x] P.2 `deletions.log` in the backup folder, written through `ClientDeletionService` (the only place that deletes a client) right after the delete. Format `id;time` per line, append-only, idempotent, safe against a torn last line, shared read. `SyncLogAsync` copies every deletion the database knows into the file at startup, after each backup and when the backup folder changes, so a write that failed earlier (unreachable folder, user is warned with a toast) or a new folder gets complete. A line with a readable id but a damaged time still counts as a deletion (time becomes "now"); only lines without an id are skipped and reported.
  - [x] P.3 Restore collects deletions from three sources (the backup's own table, the live database, the log file), merges them (earliest time wins), removes those clients with their months and photos from the restored copy in one transaction (batched, so any number of ids), and merges them into the restored database and the log. If the log exists but cannot be read, the user is asked whether to continue (cancel changes nothing). The summary dialog says how many clients were removed again and how many damaged log lines were skipped. Works for old-schema backups (they are migrated first).
- [x] P.4 The delete dialog (clients list and client page) warns that accounting and tax records may have to be kept for the legal period and suggests suspending instead; for a suspended client it also says since when.
- Still open (not blocking): a "give me all data about this client" export for access requests; privacy notice and records of processing are paperwork outside the app.

## Later (not in v1)
Notifications/reminders, emails + templates (needs notifications), list filters (search + filter flyout), CSV/Excel export (unclear what to export), tax calculator (needs the exact rules for forms of taxation, ZUS and the yearly rates before it can be designed; the old TaxDashboard code in `C:\Users\dawid\Desktop\TaxDashboard` may hold some). Backups and DB encryption have their own sections above.
