using Microsoft.EntityFrameworkCore;
using TNT.UserService.Api.Data;
using TNT.UserService.Api.DTOs;
using TNT.UserService.Api.Entities;

namespace TNT.UserService.Api.Services;

/// <summary>
/// User profile service — reads and updates the caller's own profile only.
/// The IdentityUserId is always taken from the validated JWT, never from client input.
/// </summary>
public class UserProfileService
{
    private readonly UserDbContext _db;
    private readonly ILogger<UserProfileService> _logger;

    public UserProfileService(UserDbContext db, ILogger<UserProfileService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UserProfileResponse?> GetProfileAsync(Guid identityUserId, CancellationToken ct = default)
    {
        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId, ct);

        return profile == null ? null : MapToResponse(profile);
    }

    public async Task<UserProfileResponse> UpsertProfileAsync(
        Guid identityUserId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var profile = await _db.UserProfiles
            .FirstOrDefaultAsync(p => p.IdentityUserId == identityUserId, ct);

        if (profile == null)
        {
            // Create profile on first PUT — lazy creation approach documented in Sprint01-Plan.md
            profile = new UserProfile
            {
                IdentityUserId = identityUserId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.UserProfiles.Add(profile);
            _logger.LogInformation("Created new user profile for IdentityUserId={Id}", identityUserId);
        }

        profile.DisplayName = request.DisplayName.Trim();
        profile.PhoneNumber = request.PhoneNumber?.Trim();
        profile.Address = request.Address?.Trim();
        profile.City = request.City?.Trim();
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated profile for IdentityUserId={Id}", identityUserId);
        return MapToResponse(profile);
    }

    private static UserProfileResponse MapToResponse(UserProfile p) => new()
    {
        Id = p.Id,
        IdentityUserId = p.IdentityUserId,
        DisplayName = p.DisplayName,
        PhoneNumber = p.PhoneNumber,
        Address = p.Address,
        City = p.City,
        CreatedAtUtc = p.CreatedAtUtc,
        UpdatedAtUtc = p.UpdatedAtUtc
    };
}
