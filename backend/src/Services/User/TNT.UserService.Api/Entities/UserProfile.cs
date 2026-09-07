namespace TNT.UserService.Api.Entities;

/// <summary>
/// User profile owned by the User Service.
/// IdentityUserId links this profile to the ApplicationUser in the Identity Service.
/// The User Service never queries the Identity database directly.
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The user's ID from the Identity Service JWT.
    /// Used as the correlation key between services.
    /// </summary>
    public Guid IdentityUserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
}
