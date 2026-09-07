namespace TNT.IdentityService.Api.Enums;

/// <summary>
/// Defines the available user roles in the TNT Supermarket system.
/// </summary>
public enum UserRole
{
    /// <summary>A customer who can browse and purchase products.</summary>
    Buyer = 0,

    /// <summary>A vendor who can list and manage products.</summary>
    Seller = 1,

    /// <summary>A supermarket administrator with full system access.</summary>
    Admin = 2
}
