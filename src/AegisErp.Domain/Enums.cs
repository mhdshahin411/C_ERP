namespace AegisErp.Domain;

/// <summary>Top-level classification of a chart-of-accounts account.</summary>
public enum AccountType
{
    Asset = 1,
    Liability = 2,
    Equity = 3,
    Income = 4,
    Expense = 5
}

/// <summary>The side on which an account normally carries its balance.</summary>
public enum NormalBalance
{
    Debit = 1,
    Credit = 2
}

/// <summary>Source document type for a voucher. Mirrors the modules in the prototype.</summary>
public enum VoucherType
{
    Journal = 1,
    Receipt = 2,
    Payment = 3,
    SalesInvoice = 4,
    PurchaseInvoice = 5,
    Opening = 6
}

/// <summary>Lifecycle of a voucher. Only <see cref="Posted"/> entries hit the ledger.</summary>
public enum VoucherStatus
{
    Draft = 1,
    Posted = 2,
    Rejected = 3
}

public static class AccountTypeExtensions
{
    /// <summary>Assets and expenses are debit-normal; liabilities, equity and income are credit-normal.</summary>
    public static NormalBalance NormalBalance(this AccountType type) => type switch
    {
        AccountType.Asset or AccountType.Expense => Domain.NormalBalance.Debit,
        _ => Domain.NormalBalance.Credit
    };
}
