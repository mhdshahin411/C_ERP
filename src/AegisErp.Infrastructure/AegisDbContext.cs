using AegisErp.Domain.Entities;
using AegisErp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AegisErp.Infrastructure;

public class AegisDbContext : IdentityDbContext<AppUser>
{
    public AegisDbContext(DbContextOptions<AegisDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<FiscalPeriod> FiscalPeriods => Set<FiscalPeriod>();
    public DbSet<JournalVoucher> JournalVouchers => Set<JournalVoucher>();
    public DbSet<JournalLine> JournalLines => Set<JournalLine>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceLine> SalesInvoiceLines => Set<SalesInvoiceLine>();
    public DbSet<CustomerReceipt> CustomerReceipts => Set<CustomerReceipt>();
    public DbSet<CompanySetup> CompanySetups => Set<CompanySetup>();
    public DbSet<CompanyBankAccount> CompanyBankAccounts => Set<CompanyBankAccount>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b); // Identity tables

        b.Entity<Account>(e =>
        {
            e.HasIndex(a => a.Code).IsUnique();
            e.Property(a => a.Code).HasMaxLength(20).IsRequired();
            e.Property(a => a.Name).HasMaxLength(120).IsRequired();
            e.Property(a => a.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(a => a.Category).HasMaxLength(60);
            e.Property(a => a.Currency).HasMaxLength(3).IsRequired();
            e.Property(a => a.Description).HasMaxLength(300);
            e.Ignore(a => a.NormalBalance);
            e.HasOne(a => a.Parent).WithMany().HasForeignKey(a => a.ParentId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<CostCenter>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(20).IsRequired();
            e.Property(c => c.Name).HasMaxLength(120).IsRequired();
        });

        b.Entity<FiscalPeriod>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(40).IsRequired();
        });

        b.Entity<JournalVoucher>(e =>
        {
            e.HasIndex(v => v.VoucherNo).IsUnique();
            e.Property(v => v.VoucherNo).HasMaxLength(30).IsRequired();
            e.Property(v => v.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(v => v.Narration).HasMaxLength(400);
            e.Property(v => v.Reference).HasMaxLength(60);
            e.Property(v => v.CreatedBy).HasMaxLength(80);
            e.Ignore(v => v.TotalDebit);
            e.Ignore(v => v.TotalCredit);
            e.Ignore(v => v.Difference);
            e.Ignore(v => v.IsBalanced);
            e.HasOne(v => v.FiscalPeriod).WithMany().HasForeignKey(v => v.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(v => v.Lines).WithOne(l => l.JournalVoucher).HasForeignKey(l => l.JournalVoucherId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<JournalLine>(e =>
        {
            e.Property(l => l.Debit).HasPrecision(18, 2);
            e.Property(l => l.Credit).HasPrecision(18, 2);
            e.Property(l => l.Description).HasMaxLength(300);
            e.Ignore(l => l.Signed);
            e.HasOne(l => l.Account).WithMany(a => a.Lines).HasForeignKey(l => l.AccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.CostCenter).WithMany().HasForeignKey(l => l.CostCenterId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Customer>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(20).IsRequired();
            e.Property(c => c.Name).HasMaxLength(160).IsRequired();
            e.Property(c => c.Trn).HasMaxLength(20);
            e.Property(c => c.Email).HasMaxLength(160);
            e.Property(c => c.Phone).HasMaxLength(40);
            e.Property(c => c.Address).HasMaxLength(300);
            e.Property(c => c.Group).HasMaxLength(40);
            e.Property(c => c.Currency).HasMaxLength(3).IsRequired();
            e.Property(c => c.CreditLimit).HasPrecision(18, 2);
        });

        b.Entity<SalesInvoice>(e =>
        {
            e.HasIndex(i => i.InvoiceNo).IsUnique();
            e.Property(i => i.InvoiceNo).HasMaxLength(30).IsRequired();
            e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(i => i.Narration).HasMaxLength(400);
            e.Property(i => i.CreatedBy).HasMaxLength(80);
            e.Ignore(i => i.TotalNet);
            e.Ignore(i => i.TotalVat);
            e.Ignore(i => i.TotalGross);
            e.HasOne(i => i.Customer).WithMany().HasForeignKey(i => i.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.FiscalPeriod).WithMany().HasForeignKey(i => i.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.JournalVoucher).WithMany().HasForeignKey(i => i.JournalVoucherId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(i => i.Lines).WithOne(l => l.SalesInvoice).HasForeignKey(l => l.SalesInvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<SalesInvoiceLine>(e =>
        {
            e.Property(l => l.Description).HasMaxLength(300).IsRequired();
            e.Property(l => l.Quantity).HasPrecision(18, 3);
            e.Property(l => l.UnitPrice).HasPrecision(18, 2);
            e.Property(l => l.VatRate).HasPrecision(5, 4);
            e.Ignore(l => l.Net);
            e.Ignore(l => l.Vat);
            e.Ignore(l => l.Gross);
            e.HasOne(l => l.RevenueAccount).WithMany().HasForeignKey(l => l.RevenueAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.CostCenter).WithMany().HasForeignKey(l => l.CostCenterId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<CustomerReceipt>(e =>
        {
            e.HasIndex(r => r.ReceiptNo).IsUnique();
            e.Property(r => r.ReceiptNo).HasMaxLength(30).IsRequired();
            e.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(r => r.Amount).HasPrecision(18, 2);
            e.Property(r => r.Narration).HasMaxLength(400);
            e.Property(r => r.CreatedBy).HasMaxLength(80);
            e.HasOne(r => r.Customer).WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.SalesInvoice).WithMany().HasForeignKey(r => r.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.FiscalPeriod).WithMany().HasForeignKey(r => r.FiscalPeriodId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.BankAccount).WithMany().HasForeignKey(r => r.BankAccountId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.JournalVoucher).WithMany().HasForeignKey(r => r.JournalVoucherId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<CompanySetup>(e =>
        {
            e.Property(c => c.LegalName).HasMaxLength(200).IsRequired();
            e.Property(c => c.TradeName).HasMaxLength(200);
            e.Property(c => c.CompanyCode).HasMaxLength(20);
            e.Property(c => c.LicenseNumber).HasMaxLength(60);
            e.Property(c => c.LicenseType).HasMaxLength(40);
            e.Property(c => c.Country).HasMaxLength(60);
            e.Property(c => c.Emirate).HasMaxLength(40);
            e.Property(c => c.PlaceOfIncorporation).HasMaxLength(80);
            e.Property(c => c.AccountingMethod).HasMaxLength(20);
            e.Property(c => c.FiscalYear).HasMaxLength(30);
            e.Property(c => c.BaseCurrency).HasMaxLength(3);
            e.Property(c => c.ReportingCurrency).HasMaxLength(3);
            e.Property(c => c.OrganizationLanguage).HasMaxLength(40);
            e.Property(c => c.CommunicationLanguage).HasMaxLength(120);
            e.Property(c => c.InvoiceLanguage).HasMaxLength(40);
            e.Property(c => c.TimeZone).HasMaxLength(60);
            e.Property(c => c.DateFormat).HasMaxLength(20);
            e.Property(c => c.AddressLine1).HasMaxLength(200);
            e.Property(c => c.AddressLine2).HasMaxLength(200);
            e.Property(c => c.City).HasMaxLength(80);
            e.Property(c => c.AddressEmirate).HasMaxLength(40);
            e.Property(c => c.POBox).HasMaxLength(20);
            e.Property(c => c.AddressCountry).HasMaxLength(60);
            e.Property(c => c.Phone).HasMaxLength(40);
            e.Property(c => c.Fax).HasMaxLength(40);
            e.Property(c => c.BillingAddressLine1).HasMaxLength(200);
            e.Property(c => c.BillingAddressLine2).HasMaxLength(200);
            e.Property(c => c.BillingCity).HasMaxLength(80);
            e.Property(c => c.BillingEmirate).HasMaxLength(40);
            e.Property(c => c.BillingPOBox).HasMaxLength(20);
            e.Property(c => c.BillingCountry).HasMaxLength(60);
            e.Property(c => c.TrnLabel).HasMaxLength(10);
            e.Property(c => c.TrnNumber).HasMaxLength(20);
            e.Property(c => c.VatScheme).HasMaxLength(20);
            e.Property(c => c.VatFilingFrequency).HasMaxLength(20);
            e.Property(c => c.CtTrn).HasMaxLength(20);
            e.Property(c => c.DefaultVatRate).HasPrecision(5, 2);
            e.Property(c => c.InputVatAccountCode).HasMaxLength(20);
            e.Property(c => c.OutputVatAccountCode).HasMaxLength(20);
            e.HasMany(c => c.BankAccounts).WithOne(a => a.CompanySetup)
                .HasForeignKey(a => a.CompanySetupId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CompanyBankAccount>(e =>
        {
            e.Property(a => a.BankName).HasMaxLength(120).IsRequired();
            e.Property(a => a.AccountName).HasMaxLength(160);
            e.Property(a => a.AccountNumber).HasMaxLength(40);
            e.Property(a => a.Iban).HasMaxLength(40);
            e.Property(a => a.Swift).HasMaxLength(20);
            e.Property(a => a.Currency).HasMaxLength(3);
        });
    }
}
