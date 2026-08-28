namespace TNT.IdentityService.Api.DTOs;

/// <summary>
/// Authentication response returned on successful login or token refresh.
/// Never includes PasswordHash or any secret.
/// </summary>
public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>Access token lifetime in seconds.</summary>
    public int ExpiresIn { get; set; }
    public UserSummaryResponse User { get; set; } = null!;
}

/// <summary>Safe summary of a user — never includes sensitive fields.</summary>
public class UserSummaryResponse
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
