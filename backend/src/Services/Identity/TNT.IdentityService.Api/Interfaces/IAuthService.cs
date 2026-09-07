using TNT.IdentityService.Api.DTOs;

namespace TNT.IdentityService.Api.Interfaces;

/// <summary>
/// Authentication service contract: registration, login, token refresh, logout, and current-user lookup.
/// </summary>
public interface IAuthService
{
    Task<UserSummaryResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(string rawRefreshToken, string? ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string rawRefreshToken, CancellationToken ct = default);
    Task<UserSummaryResponse> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
