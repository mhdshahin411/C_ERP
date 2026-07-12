namespace AegisErp.Domain.Entities;

/// <summary>A dimension used to tag lines by segment/department (the prototype's "cost center" column).</summary>
public class CostCenter
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
