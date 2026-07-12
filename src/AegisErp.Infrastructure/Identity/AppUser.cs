using Microsoft.AspNetCore.Identity;

namespace AegisErp.Infrastructure.Identity;

/// <summary>Application user. DisplayName is what appears on vouchers as CreatedBy.</summary>
public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>Role names used across the app. Admin ⊃ Accountant ⊃ Viewer in practice.</summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Accountant = "Accountant";
    public const string Viewer = "Viewer";

    /// <summary>Roles allowed to post documents to the ledger.</summary>
    public const string Posters = Admin + "," + Accountant;
}
