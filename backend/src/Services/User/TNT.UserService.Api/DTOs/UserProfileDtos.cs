namespace TNT.UserService.Api.DTOs;

/// <summary>Response DTO for user profile data — never includes sensitive fields.</summary>
public class UserProfileResponse
{
    public Guid Id { get; set; }
    public Guid IdentityUserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

/// <summary>Request DTO for updating the current user's profile.</summary>
public class UpdateProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
}
