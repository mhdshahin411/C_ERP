using AegisErp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AegisErp.Infrastructure.Services;

/// <summary>Loads and saves the single company-setup record (creating a demo default on first use).</summary>
public class CompanyService
{
    private readonly IDbContextFactory<AegisDbContext> _dbf;
    public CompanyService(IDbContextFactory<AegisDbContext> dbf) => _dbf = dbf;

    public async Task<CompanySetup> GetAsync()
    {
        await using var db = await _dbf.CreateDbContextAsync();
        var company = await db.CompanySetups.AsNoTracking()
            .Include(c => c.BankAccounts)
            .FirstOrDefaultAsync();

        if (company is null)
        {
            company = Default();
            db.CompanySetups.Add(company);
            await db.SaveChangesAsync();
        }
        return company;
    }

    public async Task SaveAsync(CompanySetup model)
    {
        await using var db = await _dbf.CreateDbContextAsync();
        var existing = await db.CompanySetups.Include(c => c.BankAccounts).FirstOrDefaultAsync();

        if (existing is null)
        {
            db.CompanySetups.Add(model);
            await db.SaveChangesAsync();
            return;
        }

        // Copy scalar fields; then rebuild the bank-account list.
        db.Entry(existing).CurrentValues.SetValues(model);
        db.CompanyBankAccounts.RemoveRange(existing.BankAccounts);
        existing.BankAccounts = model.BankAccounts.Select(b => new CompanyBankAccount
        {
            BankName = b.BankName,
            AccountName = b.AccountName,
            AccountNumber = b.AccountNumber,
            Iban = b.Iban,
            Swift = b.Swift,
            Currency = string.IsNullOrWhiteSpace(b.Currency) ? "AED" : b.Currency,
            IsPrimary = b.IsPrimary,
        }).ToList();

        await db.SaveChangesAsync();
    }

    private static CompanySetup Default() => new()
    {
        LegalName = "Nexus Trading FZE",
        TradeName = "Nexus Trading",
        CompanyCode = "NEXUS",
        LicenseNumber = "DXB-2019-104578",
        LicenseType = "FZE",
        RegistrationDate = new(2019, 6, 1),
        LicenseExpiryDate = new(2026, 5, 31),
        Country = "UAE",
        Emirate = "Dubai",
        PlaceOfIncorporation = "Dubai Airport Free Zone",
        IsFreeZone = true,
        IsDesignatedZone = true,
        FinancialYearStart = new(2025, 7, 1),
        FinancialYearEnd = new(2026, 6, 30),
        BooksStartDate = new(2025, 7, 1),
        AccountingMethod = "Accrual",
        FiscalYear = "Jul–Jun",
        BaseCurrency = "AED",
        ReportingCurrency = "AED",
        OrganizationLanguage = "English",
        CommunicationLanguage = "English, Arabic",
        InvoiceLanguage = "English",
        TimeZone = "Asia/Dubai",
        DateFormat = "dd/MM/yyyy",
        AddressLine1 = "Office 1204, Building A2",
        AddressLine2 = "Dubai Airport Free Zone",
        City = "Dubai",
        AddressEmirate = "Dubai",
        POBox = "54321",
        AddressCountry = "UAE",
        Phone = "+971 4 701 0000",
        BillingSameAsRegistered = true,
        VatRegistered = true,
        TrnLabel = "TRN",
        TrnNumber = "100123456700003",
        VatRegistrationDate = new(2018, 1, 1),
        VatScheme = "Standard",
        VatFilingFrequency = "Quarterly",
        CtRegistered = true,
        CtTrn = "100123456700003",
        FirstTaxPeriodStart = new(2025, 7, 1),
        FreeZonePerson = true,
        QfzpStatus = true,
        SmallBusinessRelief = false,
        DefaultVatRate = 5m,
        InputVatAccountCode = "13010",
        OutputVatAccountCode = "22010",
        MultiCompanyEnabled = false,
        AuditTrailEnabled = true,
        ApprovalWorkflowEnabled = false,
        BankAccounts = new()
        {
            new() { BankName = "Emirates NBD", AccountName = "Nexus Trading FZE", AccountNumber = "1015482930001", Iban = "AE070260001015482930001", Swift = "EBILAEAD", Currency = "AED", IsPrimary = true },
            new() { BankName = "Mashreq Bank", AccountName = "Nexus Trading FZE", AccountNumber = "019100234567", Iban = "AE930330000019100234567", Swift = "BOMLAEAD", Currency = "AED", IsPrimary = false },
        },
    };
}
