namespace TNT.IdentityService.Api.DTOs;

/// <summary>Request body for refreshing an access token.</summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Request body for logout.</summary>
public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
