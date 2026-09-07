using TNT.IdentityService.Api.Enums;

namespace TNT.IdentityService.Api.Entities;

/// <summary>
/// Represents an application user in the Identity Service.
/// PasswordHash is never returned in any API response.
/// </summary>
public class ApplicationUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    /// <summary>Raw email as entered by the user (display purposes).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Upper-cased email used for uniqueness checks (case-insensitive comparison).</summary>
    public string NormalizedEmail { get; set; } = string.Empty;

    /// <summary>BCrypt hash. NEVER exposed in API responses.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Stored as a string in the database to keep it human-readable and
    /// avoid enum-to-int mapping surprises across migrations.
    /// </summary>
    public string Role { get; set; } = UserRole.Buyer.ToString();

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
