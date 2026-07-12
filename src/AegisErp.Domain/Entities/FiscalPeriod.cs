namespace AegisErp.Domain.Entities;

/// <summary>An accounting period (e.g. "May 2026"). Vouchers post into exactly one period.</summary>
public class FiscalPeriod
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Year { get; set; }
    public int PeriodNo { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsClosed { get; set; }
}
