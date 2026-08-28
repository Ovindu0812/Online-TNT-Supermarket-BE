using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TNT.IdentityService.Api.DTOs;
using TNT.IdentityService.Api.Exceptions;
using TNT.IdentityService.Api.Interfaces;

namespace TNT.IdentityService.Api.Controllers;

/// <summary>
/// Authentication endpoints: register, login, refresh, logout, and current-user.
/// Validation is performed explicitly via injected FluentValidation validators.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator)
    {
        _authService = authService;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/register
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Register a new buyer account.</summary>
    /// <remarks>
    /// Public registration always creates a **Buyer** account.
    /// Admin and Seller roles are assigned through a protected workflow only.
    /// </remarks>
    /// <response code="201">Registration successful — returns safe user summary.</response>
    /// <response code="400">Validation error.</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new { status = 400, title = "Validation failed", errors });
        }

        var result = await _authService.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(Me), null, result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/login
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Log in with email and password. Returns a JWT access token and refresh token.</summary>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            // Return 401 for login validation errors — do not reveal which field failed
            return Unauthorized(new { status = 401, title = "Unauthorized", detail = "Invalid credentials." });
        }

        var ipAddress = GetClientIp();
        var result = await _authService.LoginAsync(request, ipAddress, ct);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/refresh
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Exchange a valid refresh token for a new access token and rotated refresh token.</summary>
    /// <response code="200">Token refreshed successfully.</response>
    /// <response code="401">Refresh token is invalid, expired, or revoked.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Unauthorized(new { detail = "Refresh token is required." });

        var ipAddress = GetClientIp();
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ipAddress, ct);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/auth/logout
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Revoke the given refresh token. Idempotent — does not fail if already revoked.</summary>
    /// <response code="204">Logout successful (or token was already revoked).</response>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [AllowAnonymous]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/auth/me
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the currently authenticated user's profile summary.</summary>
    /// <response code="200">Current user returned.</response>
    /// <response code="401">No valid JWT provided.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _authService.GetCurrentUserAsync(userId, ct);
        return Ok(result);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PRIVATE HELPERS
    // ──────────────────────────────────────────────────────────────────────────

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub")
               ?? User.FindFirstValue("uid");

        if (sub == null || !Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("Unable to determine current user from token.");

        return userId;
    }
}
