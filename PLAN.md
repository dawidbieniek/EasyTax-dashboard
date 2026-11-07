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
- Data: EF Core + SQLite. **Encryption skipped for now** (no DB password flow).
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
- `Client`: FirstName, LastName, Gender, Nip, Phone, Email, JoinDate, BankId, IsSuspended, Notes, PlanName, AuthorizationFee, SubscriptionFee, EmploymentContract, CashMethod, TaxType, PitPaymentType, IsVatPayer, VatPaymentType, UsesCashRegister, FirstCashRegisterDate, ReliefType, ReliefSinceDate
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

### M1 — Foundation (skeleton) — **awaiting your approval of the architecture**
- [x] 1.1 Solution: `EasyTax.Core`, `EasyTax.Data`, `EasyTax.App`; `.editorconfig`, `.gitattributes`, `global.json`, shared props, central package versions
- [x] 1.2 App boots: Generic Host, DI, shell window with title bar + icon-rail navigation, placeholder pages (Dashboard, Clients, Settings)
- [x] 1.3 Navigation service + page registry + view-model lifecycle
- [x] 1.4 Settings service (JSON) + theme service (System/Light/Dark, default Dark) + Settings page with theme selector and version text
- [x] 1.5 Localization via `.resw` (pl-PL) for XAML (`x:Uid`) and code (`ILocalizer`)
- [x] 1.6 Data layer wired end to end: `DbContext`, repository, initializer; Clients page reads a count from the DB
- [x] 1.7 Reorganised into feature folders (Clients, Dashboard, Settings, Shell…), Guid v7 ids, extension-block DI, `Resources/` folder, code-style rules enforced in the build (unit tests dropped)
- [ ] 1.8 Architecture approval → then replace `EnsureCreated` with migrations once the model is defined (task 2.0)
- [ ] 1.9 File logging sink

### M2 — Domain model & core logic (pure, no UI)
- [ ] 2.0 Full entities + EF configurations + first migration; seed banks and years 2026–2030
- [ ] 2.1 Relief rules: end date for Ulga na start / ZUS preferencyjny / ZUS Plus (port old `LastDayOfReduction`; mind the month-boundary cases)
- [ ] 2.2 Income sums: VAT year (prorated start in join year), cash-register year, ZUS year
- [ ] 2.3 Limits: VAT 200 000 (prorated in join year), cash register 20 000, ZUS Plus 120 000; percentage + state (OK / Warning ≥ 90% / Over)
- [ ] 2.4 Month rules: selectable months (not before join month); accounted / not / disabled state
- [ ] 2.5 Validation: NIP (10 digits + checksum), phone, email, required fields
- [ ] 2.6 Year management rules: add year, remove only when no data

### M3 — Shell
- [ ] 3.1 App bar: avatar + client picker (`AutoSuggestBox`, skips suspended), prev/next, shared `ClientContext` service (selected client, year, month) persisted between runs
- [ ] 3.2 Year picker (driven by `AccountingYear`) + month strip with green/orange/grey states
- [ ] 3.3 "Accounted" toggle bound to the selected month
- [ ] 3.4 Empty state when there are no clients

### M4 — Clients
- [ ] 4.1 Clients list: grid spike (WinUI.TableView), search, sort, suspended rows greyed, row click opens client
- [ ] 4.2 Context menu: Suspend / Unsuspend, Delete (confirm; removes month records and photo)
- [ ] 4.3 Add-client dialog with validation and live-initials avatar
- [ ] 4.4 Client page header band: avatar, name + status, contact line, chips, stats column
- [ ] 4.5 Tabs: Personal, Tax & relief, Subscription — view/edit modes, unsaved-changes guard
- [ ] 4.6 Photo: pick, resize to ~256 px, store; remove
- [ ] 4.7 Responsive container (max ~920, 2 → 1 column via AdaptiveTrigger)

### M5 — Dashboard
- [ ] 5.1 Month inputs card: income, sales/cost invoices (debounced save, 0 = clear)
- [ ] 5.2 Checklist card: per-client items (VAT-R, CEIDG-1, ZUS, Invoiced, ZUS DRA, Tax) and per-month items (JPK V7, VAT UE), with timestamps
- [ ] 5.3 Notes card (autosave on pause)
- [ ] 5.4 Gauges: VAT, cash register, ZUS Plus (or "not applicable"), warning/over colours, tooltip
- [ ] 5.5 Income line chart + invoices bar chart for the selected year; live updates
- [ ] 5.6 Layout: 3 gauges on top, charts left, cards right, equal column height; narrow-window behaviour

### M6 — Settings
- [ ] 6.1 Years management UI (add / remove where allowed)

### M7 — Polish & release
- [ ] 7.1 Keyboard: Ctrl+F search, Ctrl+S save, arrow navigation for clients/months
- [ ] 7.2 Accessibility pass (automation names, contrast, focus order)
- [ ] 7.3 Error handling + friendly messages for DB errors
- [ ] 7.4 Packaging/installer and auto-update decision
- [ ] 7.5 Demo data command for development

## Later (not in v1)
DB encryption, backups, notifications/reminders, emails + templates, tax calculator, list filters (search + filter flyout), CSV/Excel export.
