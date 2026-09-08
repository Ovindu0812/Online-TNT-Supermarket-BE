using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace TNT.IdentityService.Api.Controllers;

/// <summary>
/// Sprint 01 role-based authorization demonstration endpoints.
/// These endpoints exist only to verify JWT and role authorization during Sprint 01.
/// They may be removed in future sprints.
/// </summary>
[ApiController]
[Route("api/test")]
[Produces("application/json")]
public class TestAuthController : ControllerBase
{
    /// <summary>Accessible only to authenticated users with the Buyer role.</summary>
    /// <response code="200">Access granted.</response>
    /// <response code="401">Not authenticated.</response>
    /// <response code="403">Authenticated but does not have the Buyer role.</response>
    [HttpGet("buyer")]
    [Authorize(Roles = "Buyer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult BuyerEndpoint() =>
        Ok(new { message = "Hello Buyer! You have the Buyer role.", role = "Buyer" });

    /// <summary>Legacy test endpoint for the existing Seller role.</summary>
    [HttpGet("seller")]
    [Authorize(Roles = "Seller")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult SellerEndpoint() =>
        Ok(new { message = "Hello Seller! You have the Seller role.", role = "Seller" });

    /// <summary>Accessible to authenticated users with the Staff or Manager role.</summary>
    [HttpGet("staff")]
    [Authorize(Roles = "Staff,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult StaffEndpoint() =>
        Ok(new { message = "Staff operations access granted." });

    /// <summary>Accessible only to authenticated users with the Admin role.</summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminEndpoint() =>
        Ok(new { message = "Hello Admin! You have the Admin role.", role = "Admin" });

    /// <summary>Accessible only to authenticated users with the Rider role.</summary>
    [HttpGet("rider")]
    [Authorize(Roles = "Rider")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult RiderEndpoint() =>
        Ok(new { message = "Rider operations access granted.", role = "Rider" });

    /// <summary>Accessible to any authenticated user regardless of role.</summary>
    [HttpGet("authenticated")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult AuthenticatedEndpoint() =>
        Ok(new { message = "You are authenticated!", user = User.Identity?.Name });
}
