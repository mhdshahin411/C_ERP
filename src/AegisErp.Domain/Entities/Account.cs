namespace AegisErp.Domain.Entities;

/// <summary>A chart-of-accounts account. Non-postable accounts act as headers/roll-ups.</summary>
public class Account
{
    public int Id { get; set; }

    /// <summary>Human-facing account number, e.g. "11020". Unique.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public AccountType Type { get; set; }

    /// <summary>False for header accounts that only aggregate children.</summary>
    public bool IsPostable { get; set; } = true;

    public int? ParentId { get; set; }
    public Account? Parent { get; set; }

    /// <summary>Optional sub-classification, e.g. "Cash and cash equivalents", "Accounts receivable".</summary>
    public string? Category { get; set; }

    /// <summary>Account currency. AED-only for now (stored, no FX logic).</summary>
    public string Currency { get; set; } = "AED";

    /// <summary>Optional free-text notes.</summary>
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public NormalBalance NormalBalance => Type.NormalBalance();

    public ICollection<JournalLine> Lines { get; set; } = new List<JournalLine>();
}
