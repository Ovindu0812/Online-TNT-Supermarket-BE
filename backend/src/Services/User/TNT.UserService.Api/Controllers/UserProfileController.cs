using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TNT.UserService.Api.DTOs;
using TNT.UserService.Api.Services;

namespace TNT.UserService.Api.Controllers;

/// <summary>
/// User profile endpoints.
/// All endpoints read the caller's identity from the JWT — the user ID is never taken from the request body.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UserProfileController : ControllerBase
{
    private readonly UserProfileService _profileService;
    private readonly ILogger<UserProfileController> _logger;

    public UserProfileController(UserProfileService profileService, ILogger<UserProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/users/me
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the current user's profile.</summary>
    /// <response code="200">Profile found.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="404">Profile does not exist yet.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var profile = await _profileService.GetProfileAsync(userId, ct);

        if (profile == null)
        {
            return NotFound(new { message = "Profile not found. Use PUT /api/users/me to create it." });
        }

        return Ok(profile);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUT /api/users/me
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates or updates the current user's profile.</summary>
    /// <response code="200">Profile updated/created successfully.</response>
    /// <response code="401">Not authenticated.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _profileService.UpsertProfileAsync(userId, request, ct);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ──────────────────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? User.FindFirstValue("uid");

        if (sub == null || !Guid.TryParse(sub, out var userId))
        {
            throw new InvalidOperationException("Unable to determine current user from token.");
        }

        return userId;
    }
}
