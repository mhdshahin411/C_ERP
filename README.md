# NexusERP

A full-stack ERP for finance & operations, being built out from the original
single-file HTML prototype (`aegis_erp_bc 17.05.2026.html`).

**Stack:** ASP.NET Core 8 · Blazor Server · MudBlazor · Entity Framework Core ·
ASP.NET Core Identity · PostgreSQL (production) / SQLite (zero-setup local dev).

The repository contains a **working General Ledger core**, a **complete Accounts
Receivable slice**, and **cookie-based authentication with roles**. Every posting flow
runs through one domain posting engine inside a database transaction, so the books are
balanced by construction.

## What works today

| Screen | Route | Access |
|---|---|---|
| Sign in / access denied | `/Account/Login`, `/Account/AccessDenied` | anonymous |
| Dashboard (KPI cards) | `/` | any signed-in user |
| Chart of Accounts | `/chart-of-accounts` | any signed-in user |
| General Ledger | `/general-ledger` | any signed-in user |
| Trial Balance | `/trial-balance` | any signed-in user |
| Journal Voucher | `/journal-voucher` | Admin, Accountant |
| Customers (+ statement drill-down) | `/customers` | any signed-in user (create: posters) |
| Sales Invoice → **auto-generates GL voucher** | `/sales-invoice` | Admin, Accountant |
| Receipt Voucher (invoice allocation or on-account) | `/receipt-voucher` | Admin, Accountant |
| AR Aging (buckets by days past due) | `/ar-aging` | any signed-in user |
| Users & Roles | `/users` | Admin |
| Remaining modules | `/soon/{name}` | placeholders |

### Demo accounts (seeded)

| Email | Password | Roles |
|---|---|---|
| admin@nexuserp.com | Admin@123! | Admin, Accountant |
| finance@nexuserp.com | Finance@123! | Accountant |
| viewer@nexuserp.com | Viewer@123! | Viewer (read-only) |

The database is seeded on first run with the demo company (Nexus Trading FZE, AED): chart of
accounts, opening balances, three customers, four sales invoices and two receipts —
all posted through the real double-entry engine, so the trial balance is in balance
and the AR aging shows a live overdue picture.

## Architecture

```
AegisErp.sln
src/
  AegisErp.Domain          # Entities + posting rules (pure C#, no dependencies)
  AegisErp.Infrastructure  # EF Core DbContext (+Identity stores), posting engine, services, seed
  AegisErp.Web             # Blazor Server UI (MudBlazor), auth wiring, pages
tests/
  AegisErp.Tests           # xunit: posting rules, invoice/receipt flows, aging buckets
```

Key design points:

- **One posting engine.** `JournalPoster.PostAsync` builds and validates every GL
  voucher (period open, date within period, debits = credits) on the *caller's*
  DbContext without committing. Document services (`SalesInvoiceService`,
  `ReceiptService`, `JournalService`) wrap it in their own transaction so a document
  and its voucher are saved atomically — an invoice can never exist without its GL entry.
- **Shared document numbers.** An invoice and its voucher share one number
  (`INV-2026-0143`); numbering continues from the highest existing suffix per year.
- **Subledger over the GL.** Customer balances, statements and aging are computed from
  posted AR documents; the AR control account (12010) holds the GL side. Receipts can
  be allocated to an invoice (capped at its outstanding) or left on account.
- **Auth.** Static-SSR login page (render-mode gated under `/Account`), cookie Identity,
  role-gated pages via `[Authorize]`, security-stamp revalidation on long-lived circuits.

## Run it

```bash
dotnet run --project src/AegisErp.Web
```

Open the URL it prints and sign in with a demo account. First launch creates
`aegis_erp.db` (SQLite) and seeds everything.

Run the tests:

```bash
dotnet test
```

## Switching to PostgreSQL

Edit `src/AegisErp.Web/appsettings.json` — no code changes needed:

```json
"Database": { "Provider": "Postgres" },
"ConnectionStrings": {
  "Postgres": "Host=localhost;Database=aegis_erp;Username=postgres;Password=yourpassword"
}
```

## Migrations (recommended before production)

The app uses `EnsureCreated()` for a friction-free start — note that schema changes
require deleting the dev `aegis_erp.db`. Before production, switch to EF migrations
(`dotnet-ef` is pinned in `.config/dotnet-tools.json`):

```bash
dotnet tool restore
$env:AEGIS_PROVIDER="Postgres"
dotnet ef migrations add InitialCreate -p src/AegisErp.Infrastructure -s src/AegisErp.Web
dotnet ef database update            -p src/AegisErp.Infrastructure -s src/AegisErp.Web
```

Then replace `EnsureCreatedAsync()` in `SeedData` with `db.Database.MigrateAsync()`.

## Known production-hardening items

These are deliberately deferred (they don't affect single-user SQLite dev, and the full
fix is PostgreSQL-specific):

- **Concurrent document numbering.** `JournalPoster.NextDocNoAsync` computes the next number
  as `max(existing) + 1`. Under Postgres' default isolation, two simultaneous posts can
  compute the same number. The losing writer now fails cleanly with a recoverable
  `PostingException` (unique index + `SaveAndCommitAsync` translation) rather than crashing,
  but the proper fix is a per-(prefix, year) advisory lock or a Serializable transaction with
  retry around number allocation.
- **Concurrent receipt allocation.** `ReceiptService` checks an invoice's outstanding balance
  before inserting. Two simultaneous receipts against the same invoice could both pass on
  Postgres (there's no DB constraint backing the invariant). Fix: lock the invoice row
  (`FOR UPDATE`) inside the transaction, or persist an `AllocatedTotal` column with a
  `CHECK (AllocatedTotal <= gross)`.

## Roadmap (next slices, same pattern)

1. **AP** — Vendors, Purchase Invoice, Payment Voucher (mirror of AR).
2. Credit notes / debit notes and receipt allocation across multiple invoices.
3. Approval workflows (the prototype's Initiator → Finance → CFO → Posted chains).
4. Period close + P&L/Balance Sheet/Cash Flow computed from the ledger.
5. Inventory, HR/Payroll (WPS), Fixed Assets with depreciation runs.
6. Production hardening: EF migrations, HTTPS/hosting, backups, real user management UI.
