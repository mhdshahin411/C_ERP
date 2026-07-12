using AegisErp.Domain;
using AegisErp.Domain.Entities;
using AegisErp.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AegisErp.Tests;

/// <summary>
/// SQLite in-memory database shared across contexts through one open connection,
/// pre-seeded with the minimal chart of accounts, a period and a customer.
/// </summary>
public sealed class TestDb : IDbContextFactory<AegisDbContext>, IDisposable
{
    private readonly SqliteConnection _connection;

    public FiscalPeriod May { get; }
    public FiscalPeriod Jun { get; }
    public Account Bank { get; }
    public Account Ar { get; }
    public Account Vat { get; }
    public Account Revenue { get; }
    public Account Expense { get; }
    public Customer Customer { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = CreateDbContext();
        db.Database.EnsureCreated();

        May = new FiscalPeriod { Name = "May 2026", Year = 2026, PeriodNo = 11, StartDate = new(2026, 5, 1), EndDate = new(2026, 5, 31) };
        Jun = new FiscalPeriod { Name = "Jun 2026", Year = 2026, PeriodNo = 12, StartDate = new(2026, 6, 1), EndDate = new(2026, 6, 30) };
        Bank = new Account { Code = "11020", Name = "Bank", Type = AccountType.Asset };
        Ar = new Account { Code = WellKnownAccounts.AccountsReceivable, Name = "Accounts Receivable", Type = AccountType.Asset };
        Vat = new Account { Code = WellKnownAccounts.VatPayable, Name = "VAT Payable", Type = AccountType.Liability };
        Revenue = new Account { Code = "41010", Name = "Revenue", Type = AccountType.Income };
        Expense = new Account { Code = "51010", Name = "Expense", Type = AccountType.Expense };
        Customer = new Customer { Code = "C-0001", Name = "Test Customer", PaymentTermsDays = 30 };

        db.FiscalPeriods.AddRange(May, Jun);
        db.Accounts.AddRange(Bank, Ar, Vat, Revenue, Expense);
        db.Customers.Add(Customer);
        db.SaveChanges();
    }

    public AegisDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AegisDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new AegisDbContext(options);
    }

    public Task<AegisDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(CreateDbContext());

    public void Dispose() => _connection.Dispose();
}
