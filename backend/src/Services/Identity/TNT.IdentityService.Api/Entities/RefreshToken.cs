namespace TNT.IdentityService.Api.Entities;

/// <summary>
/// Represents a refresh token associated with an application user.
/// Only a BCrypt/SHA256 hash of the token is stored — the raw value is never persisted.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>SHA-256 hash of the raw refresh token. The raw token is NEVER stored.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>Hash of the token that replaced this one during rotation.</summary>
    public string? ReplacedByTokenHash { get; set; }

    /// <summary>IP address that created this token (for auditing).</summary>
    public string? CreatedByIp { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Returns true if this token is still valid.</summary>
    public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;

    /// <summary>Returns true if the token has been revoked.</summary>
    public bool IsRevoked => RevokedAtUtc != null;

    /// <summary>Returns true if the token has expired.</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
}
