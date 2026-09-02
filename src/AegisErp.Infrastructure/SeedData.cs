using AegisErp.Domain;
using AegisErp.Domain.Entities;
using AegisErp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AegisErp.Infrastructure;

/// <summary>
/// Seeds a demo company (Nexus Trading FZE) matching the HTML prototype: roles and users, a chart
/// of accounts, cost centres, fiscal periods, an opening-balance voucher, customers, and
/// AR documents (invoices + receipts) whose GL vouchers are generated through the same
/// domain posting rules the app uses — so the books are balanced by construction.
/// </summary>
public static class SeedData
{
    public static async Task EnsureSeededAsync(
        AegisDbContext db, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        await db.Database.EnsureCreatedAsync();
        await SeedIdentityAsync(userManager, roleManager);

        if (await db.Accounts.AnyAsync()) return; // business data already seeded

        var now = DateTime.UtcNow;

        // ── Fiscal periods (fiscal year runs Jul–Jun; May 2026 is period 11) ──
        var apr = Period("Apr 2026", 10, new(2026, 4, 1), new(2026, 4, 30));
        var may = Period("May 2026", 11, new(2026, 5, 1), new(2026, 5, 31));
        var jun = Period("Jun 2026", 12, new(2026, 6, 1), new(2026, 6, 30));
        var jul = Period("Jul 2026", 1, new(2026, 7, 1), new(2026, 7, 31));
        db.FiscalPeriods.AddRange(apr, may, jun, jul);

        // ── Cost centres (segments) ──
        var ccAdmin = new CostCenter { Code = "ADM", Name = "Admin" };
        var ccPass = new CostCenter { Code = "PASS", Name = "Passport Services" };
        var ccTravel = new CostCenter { Code = "TRV", Name = "Travel & Tourism" };
        var ccHr = new CostCenter { Code = "HR", Name = "HR" };
        db.CostCenters.AddRange(ccAdmin, ccPass, ccTravel, ccHr);

        // ── Chart of accounts (hierarchical: header groups + postable children) ──
        Account Hdr(string code, string name, AccountType type, Account? parent = null) =>
            new() { Code = code, Name = name, Type = type, IsPostable = false, Parent = parent };
        Account A(string code, string name, AccountType type, Account parent, string category) =>
            new() { Code = code, Name = name, Type = type, IsPostable = true, Parent = parent, Category = category };

        var hAssets = Hdr("10000", "Assets", AccountType.Asset);
        var hCurrent = Hdr("11000", "Current Assets", AccountType.Asset, hAssets);
        var hFixed = Hdr("15000", "Fixed Assets", AccountType.Asset, hAssets);
        var hLiab = Hdr("20000", "Liabilities", AccountType.Liability);
        var hEquity = Hdr("30000", "Equity", AccountType.Equity);
        var hRevenue = Hdr("40000", "Revenue", AccountType.Income);
        var hExpense = Hdr("50000", "Expenses", AccountType.Expense);

        var accounts = new List<Account>
        {
            hAssets, hCurrent, hFixed, hLiab, hEquity, hRevenue, hExpense,
            A("11020", "ENBD Current Account", AccountType.Asset, hCurrent, "Cash and cash equivalents"),
            A("11030", "Mashreq Bank Account", AccountType.Asset, hCurrent, "Cash and cash equivalents"),
            A(WellKnownAccounts.AccountsReceivable, "Accounts Receivable", AccountType.Asset, hCurrent, "Accounts receivable"),
            A("13010", "Prepaid Expenses & VAT Input", AccountType.Asset, hCurrent, "Current asset"),
            A("15010", "Property & Equipment", AccountType.Asset, hFixed, "Fixed asset"),
            A("21010", "Accounts Payable", AccountType.Liability, hLiab, "Accounts payable"),
            A(WellKnownAccounts.VatPayable, "VAT Payable", AccountType.Liability, hLiab, "Current liability"),
            A("31010", "Share Capital & Retained Earnings", AccountType.Equity, hEquity, "Equity"),
            A("41010", "Passport Services Revenue", AccountType.Income, hRevenue, "Income"),
            A("41020", "Travel & Tourism Revenue", AccountType.Income, hRevenue, "Income"),
            A("51010", "Salaries & Wages", AccountType.Expense, hExpense, "Direct expense"),
            A("51020", "Office Rent", AccountType.Expense, hExpense, "Indirect expense"),
        };
        db.Accounts.AddRange(accounts);

        // ── Customers ──
        var emirates = new Customer { Code = "C-0001", Name = "Emirates Group", Group = "Corporate", CreditLimit = 500_000, Trn = "100234567890003", Email = "ap@emirates.example", Phone = "+971 4 708 1111", PaymentTermsDays = 30 };
        var spiceJet = new Customer { Code = "C-0002", Name = "Spice Jet Ltd", Group = "Corporate", CreditLimit = 300_000, Trn = "100345678900003", Email = "finance@spicejet.example", Phone = "+971 4 555 2222", PaymentTermsDays = 30 };
        var dnata = new Customer { Code = "C-0003", Name = "DNATA Travel", Group = "Corporate", CreditLimit = 250_000, Trn = "100456789010003", Email = "accounts@dnata.example", Phone = "+971 4 316 6666", PaymentTermsDays = 45 };
        db.Customers.AddRange(emirates, spiceJet, dnata);

        await db.SaveChangesAsync();

        var byCode = accounts.ToDictionary(a => a.Code);
        int Acc(string code) => byCode[code].Id;

        // ── Opening balances (balanced voucher dated 30 Apr) ──
        AddVoucher(db, VoucherType.Opening, "OB-2026-0001", new(2026, 4, 30), apr.Id,
            "Opening balances — carried forward", now, new[]
            {
                Line(Acc("11020"), ccAdmin.Id, "ENBD opening", 1_130_000, 0),
                Line(Acc("11030"), ccAdmin.Id, "Mashreq opening", 820_000, 0),
                Line(Acc("12010"), ccAdmin.Id, "AR opening (on account)", 1_650_000, 0),
                Line(Acc("13010"), ccAdmin.Id, "Prepaid/VAT input opening", 412_040, 0),
                Line(Acc("15010"), ccAdmin.Id, "Fixed assets opening", 2_100_000, 0),
                Line(Acc("21010"), ccAdmin.Id, "AP opening", 0, 866_333),
                Line(Acc("22010"), ccAdmin.Id, "VAT payable opening", 0, 42_000),
                Line(Acc("31010"), ccAdmin.Id, "Equity balancing figure", 0, 5_203_707),
            });

        // ── May 2026 expenses (rent + payroll) ──
        AddVoucher(db, VoucherType.Payment, "PV-2026-0038", new(2026, 5, 5), may.Id,
            "May 2026 office rent — Al Futtaim Properties", now, new[]
            {
                Line(Acc("51020"), ccAdmin.Id, "Office rent", 79_167, 0),
                Line(Acc("11020"), ccAdmin.Id, "Bank payment", 0, 79_167),
            });
        AddVoucher(db, VoucherType.Journal, "JV-2026-0001", new(2026, 5, 25), may.Id,
            "May 2026 payroll run", now, new[]
            {
                Line(Acc("51010"), ccHr.Id, "Salaries & wages", 84_645, 0),
                Line(Acc("11020"), ccHr.Id, "Net pay disbursed", 0, 84_645),
            });

        // ── Sales invoices (each generates its GL voucher) ──
        var inv141 = AddInvoice(db, "INV-2026-0141", spiceJet, new(2026, 5, 3), may.Id, now,
            "Passport processing — Spice Jet Ltd", "Ahmed Al Mansoori", byCode, ccPass.Id,
            ("Passport processing services (100 applications)", "41010", 100, 850, 0.05m));

        var inv142 = AddInvoice(db, "INV-2026-0142", emirates, new(2026, 5, 5), may.Id, now,
            "Corporate travel package — Emirates Group", "Ahmed Al Mansoori", byCode, ccTravel.Id,
            ("Corporate travel package — May", "41020", 1, 90_000, 0.05m));

        var inv143 = AddInvoice(db, "INV-2026-0143", dnata, new(2026, 6, 15), jun.Id, now,
            "Visa processing retainer — DNATA", "Ahmed Al Mansoori", byCode, ccPass.Id,
            ("Visa processing retainer — June", "41010", 1, 40_000, 0.05m));

        AddInvoice(db, "INV-2026-0144", dnata, new(2026, 7, 5), jul.Id, now,
            "Travel desk services — DNATA", "Ahmed Al Mansoori", byCode, ccTravel.Id,
            ("Travel desk services — July", "41020", 1, 30_000, 0.05m));

        // ── Customer receipts ──
        AddReceipt(db, "RV-2026-0028", emirates, inv142, new(2026, 5, 8), may.Id, Acc("11020"),
            94_500, "Receipt — Emirates Group settles INV-2026-0142", now, byCode);
        AddReceipt(db, "RV-2026-0029", dnata, inv143, new(2026, 6, 28), jun.Id, Acc("11020"),
            20_000, "Part payment — DNATA against INV-2026-0143", now, byCode);

        await db.SaveChangesAsync();
    }

    // ────────────────────────────────────────────────────────────────────

    private static async Task SeedIdentityAsync(UserManager<AppUser> users, RoleManager<IdentityRole> roles)
    {
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Accountant, AppRoles.Viewer })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        await EnsureUserAsync(users, "admin@nexuserp.com", "Admin@123!", "System Admin", AppRoles.Admin, AppRoles.Accountant);
        await EnsureUserAsync(users, "finance@nexuserp.com", "Finance@123!", "Fatima Al Rashidi", AppRoles.Accountant);
        await EnsureUserAsync(users, "viewer@nexuserp.com", "Viewer@123!", "Ahmed Al Mansoori", AppRoles.Viewer);
    }

    private static async Task EnsureUserAsync(
        UserManager<AppUser> users, string email, string password, string displayName, params string[] roles)
    {
        if (await users.FindByNameAsync(email) is not null) return;
        var user = new AppUser { UserName = email, Email = email, DisplayName = displayName, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                $"Failed to seed user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        await users.AddToRolesAsync(user, roles);
    }

    private static FiscalPeriod Period(string name, int no, DateOnly start, DateOnly end) =>
        new() { Name = name, Year = 2026, PeriodNo = no, StartDate = start, EndDate = end };

    private static JournalLine Line(int accountId, int? costCenterId, string desc, decimal dr, decimal cr) =>
        new() { AccountId = accountId, CostCenterId = costCenterId, Description = desc, Debit = dr, Credit = cr };

    private static JournalVoucher AddVoucher(AegisDbContext db, VoucherType type, string no, DateOnly date,
        int periodId, string narration, DateTime now, JournalLine[] lines)
    {
        var v = new JournalVoucher
        {
            VoucherNo = no,
            Type = type,
            Date = date,
            FiscalPeriodId = periodId,
            Narration = narration,
            CreatedBy = "System Admin",
            CreatedAtUtc = now,
        };
        var n = 1;
        foreach (var l in lines) { l.LineNo = n++; v.Lines.Add(l); }
        v.Post(now); // enforces balance even for seed data
        db.JournalVouchers.Add(v);
        return v;
    }

    private static SalesInvoice AddInvoice(AegisDbContext db, string no, Customer customer, DateOnly date,
        int periodId, DateTime now, string narration, string createdBy,
        Dictionary<string, Account> byCode, int costCenterId,
        params (string Desc, string RevenueCode, decimal Qty, decimal Price, decimal VatRate)[] lines)
    {
        var invoice = new SalesInvoice
        {
            InvoiceNo = no,
            CustomerId = customer.Id,
            Date = date,
            DueDate = date.AddDays(customer.PaymentTermsDays),
            FiscalPeriodId = periodId,
            Narration = narration,
            CreatedBy = createdBy,
            CreatedAtUtc = now,
        };
        var n = 1;
        foreach (var l in lines)
            invoice.Lines.Add(new SalesInvoiceLine
            {
                LineNo = n++,
                Description = l.Desc,
                RevenueAccountId = byCode[l.RevenueCode].Id,
                CostCenterId = costCenterId,
                Quantity = l.Qty,
                UnitPrice = l.Price,
                VatRate = l.VatRate,
            });
        invoice.Post(now);

        // Same voucher shape the SalesInvoiceService generates.
        var jLines = new List<JournalLine>
        {
            Line(byCode[WellKnownAccounts.AccountsReceivable].Id, null, $"{customer.Name} — {no}", invoice.TotalGross, 0),
        };
        jLines.AddRange(invoice.Lines.Select(l =>
            Line(l.RevenueAccountId, l.CostCenterId, l.Description, 0, l.Net)));
        if (invoice.TotalVat > 0)
            jLines.Add(Line(byCode[WellKnownAccounts.VatPayable].Id, null, $"Output VAT — {no}", 0, invoice.TotalVat));

        invoice.JournalVoucher = AddVoucher(db, VoucherType.SalesInvoice, no, date, periodId, narration, now, jLines.ToArray());
        db.SalesInvoices.Add(invoice);
        return invoice;
    }

    private static void AddReceipt(AegisDbContext db, string no, Customer customer, SalesInvoice? invoice,
        DateOnly date, int periodId, int bankAccountId, decimal amount, string narration, DateTime now,
        Dictionary<string, Account> byCode)
    {
        var receipt = new CustomerReceipt
        {
            ReceiptNo = no,
            CustomerId = customer.Id,
            SalesInvoice = invoice,
            Date = date,
            FiscalPeriodId = periodId,
            BankAccountId = bankAccountId,
            Amount = amount,
            Narration = narration,
            CreatedBy = "System Admin",
            CreatedAtUtc = now,
        };
        receipt.Post(now);

        receipt.JournalVoucher = AddVoucher(db, VoucherType.Receipt, no, date, periodId, narration, now, new[]
        {
            Line(bankAccountId, null, narration, amount, 0),
            Line(byCode[WellKnownAccounts.AccountsReceivable].Id, null, $"{customer.Name} — {no}", 0, amount),
        });
        db.CustomerReceipts.Add(receipt);
    }
}
