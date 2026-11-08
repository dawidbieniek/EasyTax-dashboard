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
- Data: EF Core + SQLite. **Encryption skipped for now** (no DB password flow); planned later with SQLCipher, see "Database encryption".
- Charts/gauges: LiveChartsCore.SkiaSharpView.WinUI (spike verified). Added with the dashboard milestone.
- Grid: WinUI.TableView candidate; verify in the clients-list task.
- UI language: **Polish**, all strings in `.resw` resources (English can be added later).
- Photos: optional, stored in the DB (separate table), shown with `PersonPicture`.
- Years are **user-managed**: seeded 2026–2030, user can add years and remove a year only if it has no data.
- Settings page: theme (System / Light / **Dark default**) and app version shown as plain text. No DB settings.
- **Accent colour = the Windows system accent** (WinUI's default), so the app follows the user's Windows personalisation. No colour setting, no custom palette.
- Out of scope for v1: backups, notifications, email, tax calculator, list filters, DB encryption.

## Data model (draft)
One `MonthRecord` per client per month replaces the old app's five child tables.

- All entities derive from `Entity` (`Guid Id`, v7). `AccountingYear` is keyed by its year number instead.
- `Client`: FirstName, LastName, Gender, Nip, Phone, Email, JoinDate, BankId, IsSuspended, Notes, PlanName, AuthorizationFee, SubscriptionFee, EmploymentContract, CashMethod, TaxType, PitPaymentFrequency, IsVatPayer, VatPaymentFrequency, UsesCashRegister, FirstCashRegisterDate, ReliefType, ReliefSinceDate (dates are `DateOnly`; enums are stored as text)
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
- [ ] 7.4 Packaging and updates. **Decided: packaged MSIX with an `.appinstaller`** (on a fileshare or web URL) for Windows' built-in "check remote version and update", **signed with a self-signed certificate**. Do it before the first real release, because packaged apps get redirected, private app data and switching later moves the data folder and the wrapped encryption key.
  - Create the certificate once (subject = the manifest `Publisher`); keep the `.pfx` in the admin's password manager; install the public `.cer` into "Trusted People" on each machine (one-time, admin).
  - Keep the same certificate subject and package identity for the life of the app. A renewed or recreated certificate with the same subject still counts as an update, but must be trusted again on every machine.
  - If the app is ever installed on machines the admin does not control, move to Trusted Signing or a commercial certificate first (the subject text must stay the same).
  - Test an update with a real encrypted DB before shipping. Uninstall deletes the data folder, so the uninstall docs must point at backups and the recovery key.
- [x] 7.5 Demo data for development: `EasyTax.exe --seed-demo` (Debug builds only) fills an empty database with five sample clients and 2026 month records. `EASYTAX_DATA_DIR` redirects the data folder (database and settings), so testing never touches real data.

## Backups (planned, post-v1)
Settled with the user; not scheduled in a milestone yet.

**Prerequisite: single instance**
- [ ] B.0 Enforce one app instance (`AppInstance.FindOrRegisterForKey`). A second launch activates the first and exits. Guarantees backup and restore never overlap.

**Settings** (new card on the Settings page)
- `BackupFolder` (default `Documents\EasyTax\Backups`, user-configurable, a fileshare path must work as plain file copy) and a scheduled-backups on/off toggle. Interval fixed at 7 days. No retention/pruning: keep all.

**Backup flow** (`Backups/` feature folder, `BackupService`, plain methods with guard clauses)
- [ ] B.1 `RunAsync()`: copy the live DB with the SQLite online backup API (`SqliteConnection.BackupDatabase`) into a temp `.db`, run `PRAGMA integrity_check` on the copy, zip it with `manifest.json` (app version, schema/migration version, timestamp) as `EasyTax-yyyy-MM-dd-HHmm.zip.tmp`, then rename to `.zip` as the last step (atomic, so a `.zip` is always complete).
- [ ] B.2 Interruption safety: the live DB is only read, so closing mid-backup is harmless. On startup `DeleteStaleTempFiles()` removes leftover `.tmp` files; they never count as backups.
- [ ] B.3 Schedule on startup: `GetLatestBackup()` reads the newest valid `.zip` in the folder (the folder is the source of truth, not a stored setting). If the toggle is on and it is older than 7 days or missing, run `RunAsync()` in the background. Startup only, no timer.
- [ ] B.4 Toasts: success toast on finish; error toast on any failure, including an unreachable destination.

**Restore**
- [ ] B.5 "Restore from file": file picker for a `.zip`, confirmation dialog saying the current data will be replaced, unzip to temp, integrity check, manifest check (refuse a newer schema, migrate an older one), safety backup of the current DB, close connections, swap the file, restart the app.

**Settings UI** (B.6): folder path + Browse, scheduled toggle, last backup time/status, "Back up now" and "Restore from file" buttons.

Out of scope: retention, off-machine copies, Windows scheduled tasks. Encryption has its own section below; once it lands, backups are encrypted copies of the encrypted DB (B.1 opens both ends of `BackupDatabase` with the key; B.5 needs the key to run its integrity check, and asks for the recovery key when the local one cannot open the file).

## Database encryption (planned, post-v1, ideally before the first release)
Settled with the user. Goal: a stolen `.db` file or backup `.zip` is useless without the app's key. Not protected against: someone using the user's logged-in Windows account (they already have the app). No password prompt at launch.

- **Engine:** SQLCipher (swap the SQLite native bundle, e.g. `SQLitePCLRaw.bundle_e_sqlcipher`; the password is set on the connection string). EF Core and the migrations keep working.
- **One key per install**, generated randomly on first run. The same key encrypts the DB and every backup made from it, so one recovery key opens any backup.
- **Daily use:** the key is wrapped with Windows DPAPI (`ProtectedData`, scope CurrentUser) and stored in the app data folder next to the DB. Unwrapped silently on start; no prompt.
- **Recovery key:** the same key is shown to the user once, as a grouped code (e.g. `A1B2-C3D4-...`), so it survives a dead machine or a lost Windows profile. If the user loses both the machine and the recovery key, the data is gone. This is by design and the setup dialog says so plainly.
- **Not chosen:** hardcoded key (extractable from the binary, one leak exposes every install), key fetched from a server (needs a service plus per-install enrollment; too heavy), admin-typed key at setup (fallback if the user flow proves too awkward).

**Tasks**
- [ ] E.0 Spike: confirm the SQLCipher package's licence and that it works with .NET 10, unpackaged WinUI 3 and EF Core 10 migrations. Stop and revisit if not.
- [ ] E.1 `DatabaseKeyService` (in `Infrastructure/` or a `Security/` feature folder): generate the key, wrap and unwrap it with DPAPI, load it at startup before the DB opens. Wired into the connection string in `EasyTax.Data`. **Rule: never generate a new key when a DB file already exists** (an update, a lost key file or a new profile must go to the recovery flow E.4, never silently create a key that would make the existing DB unreadable).
- [ ] E.2 First-run dialog: shows the recovery key with Copy, Save to file and Print; cannot be skipped; the user retypes a few characters to confirm they stored it. Explains that losing the key and the machine means losing the data.
- [ ] E.3 Settings card: "Show recovery key" (the user is already inside the app, so showing it again is fine).
- [ ] E.4 Recovery flow: when the local key is missing or cannot open the DB (new machine, new Windows profile), ask for the recovery key, verify it by opening the DB, then re-wrap it with DPAPI for this machine.
- [ ] E.5 Existing plain databases: demo DBs are disposable, so no migration path; an unencrypted file is simply rejected with a clear message.
- [ ] E.6 Backup tasks B.1 and B.5 updated for the encrypted DB (see Backups).

Open decisions (defaults assumed unless changed): the key format is a grouped code; the recovery-key step in first-run setup is mandatory.

## Later (not in v1)
Notifications/reminders, emails + templates (needs notifications), list filters (search + filter flyout), CSV/Excel export (unclear what to export), tax calculator (needs the exact rules for forms of taxation, ZUS and the yearly rates before it can be designed; the old TaxDashboard code in `C:\Users\dawid\Desktop\TaxDashboard` may hold some). Backups and DB encryption have their own sections above.
