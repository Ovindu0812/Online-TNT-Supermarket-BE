using TNT.IdentityService.Api.Entities;

namespace TNT.IdentityService.Api.Interfaces;

/// <summary>
/// Token generation and validation service.
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a short-lived JWT access token for the given user.</summary>
    string GenerateAccessToken(ApplicationUser user);

    /// <summary>
    /// Generates a cryptographically secure raw refresh token string.
    /// The caller is responsible for hashing and storing only the hash.
    /// </summary>
    string GenerateRawRefreshToken();

    /// <summary>Computes a deterministic SHA-256 hash of a raw token string.</summary>
    string HashToken(string rawToken);

    /// <summary>Returns the configured access token lifetime in seconds.</summary>
    int GetAccessTokenLifetimeSeconds();
}
