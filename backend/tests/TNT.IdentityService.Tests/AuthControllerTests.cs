using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using TNT.IdentityService.Api.Data;
using TNT.IdentityService.Api.DTOs;
using TNT.IdentityService.Api.Entities;
using TNT.IdentityService.Api.Interfaces;

namespace TNT.IdentityService.Tests;

/// <summary>
/// Integration tests for the Authentication endpoints.
/// Uses WebApplicationFactory with in-memory database and fake Kafka producer.
/// </summary>
public class AuthControllerTests : IClassFixture<IdentityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityWebApplicationFactory _factory;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthControllerTests(IdentityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper
    // ──────────────────────────────────────────────────────────────────────────

    private static RegisterRequest ValidRegisterRequest(string? email = null) => new()
    {
        FullName = "Test User",
        Email = email ?? $"test_{Guid.NewGuid():N}@example.com",
        Password = "TestPass1!",
        ConfirmPassword = "TestPass1!",
        PhoneNumber = "+1234567890"
    };

    // ──────────────────────────────────────────────────────────────────────────
    // Registration Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Register_WithValidDetails_ReturnsCreated")]
    public async Task Register_WithValidDetails_ReturnsCreated()
    {
        var request = ValidRegisterRequest();

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<UserSummaryResponse>(JsonOpts);
        body.Should().NotBeNull();
        body!.Email.Should().Be(request.Email.Trim(), because: "email is stored as entered (trimmed)");
        body.Role.Should().Be("Buyer", because: "public registration always creates a Buyer");
        body.Id.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Register_WithInvalidEmail_ReturnsBadRequest")]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = "not-an-email",
            Password = "TestPass1!",
            ConfirmPassword = "TestPass1!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Register_WithWeakPassword_ReturnsBadRequest")]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = $"weak_{Guid.NewGuid():N}@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Register_WithDuplicateEmail_ReturnsConflict")]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"dup_{Guid.NewGuid():N}@example.com";
        var request = ValidRegisterRequest(email);

        // First registration
        var first = await _client.PostAsJsonAsync("/api/auth/register", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        // Duplicate registration
        var second = await _client.PostAsJsonAsync("/api/auth/register", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact(DisplayName = "Register_WithMismatchedPasswords_ReturnsBadRequest")]
    public async Task Register_WithMismatchedPasswords_ReturnsBadRequest()
    {
        var request = new RegisterRequest
        {
            FullName = "Test User",
            Email = $"mismatch_{Guid.NewGuid():N}@example.com",
            Password = "TestPass1!",
            ConfirmPassword = "DifferentPass1!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(DisplayName = "Register_PublishesUserRegisteredKafkaEvent")]
    public async Task Register_PublishesUserRegisteredKafkaEvent()
    {
        // Arrange: get the fake Kafka producer from DI
        using var scope = _factory.Services.CreateScope();
        var fakeKafka = scope.ServiceProvider.GetRequiredService<IKafkaProducerService>()
            as FakeKafkaProducerService;
        fakeKafka.Should().NotBeNull();
        var initialCount = fakeKafka!.PublishedEvents.Count;

        // Act
        var request = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        fakeKafka.PublishedEvents.Count.Should().Be(initialCount + 1);
        var evt = fakeKafka.PublishedEvents.Last();
        evt.EventType.Should().Be("UserRegistered");
        evt.Role.Should().Be("Buyer");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Login Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Login_WithValidCredentials_ReturnsOkWithTokens")]
    public async Task Login_WithValidCredentials_ReturnsOkWithTokens()
    {
        var reg = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", reg);

        var loginRequest = new LoginRequest { Email = reg.Email, Password = reg.Password };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
        body.RefreshToken.Should().NotBeNullOrWhiteSpace();
        body.ExpiresIn.Should().BeGreaterThan(0);
        body.User.Role.Should().Be("Buyer");
    }

    [Fact(DisplayName = "Login_JwtContainsStoredRoleClaim")]
    public async Task Login_JwtContainsStoredRoleClaim()
    {
        var accessToken = await GetAccessTokenForRoleAsync("Staff");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        jwt.Claims.Should().ContainSingle(
            claim => (claim.Type == "role" || claim.Type == ClaimTypes.Role)
                     && claim.Value == "Staff",
            because: "the existing ApplicationUsers.Role value must be emitted as the JWT role claim");
    }

    [Fact(DisplayName = "Login_WithInvalidCredentials_ReturnsUnauthorized")]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPass1!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Login_WithWrongPassword_ReturnsUnauthorized")]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var reg = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", reg);

        var loginRequest = new LoginRequest { Email = reg.Email, Password = "WrongPass1!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Protected Endpoint Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "ProtectedEndpoint_WithoutJwt_ReturnsUnauthorized")]
    public async Task ProtectedEndpoint_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "ProtectedEndpoint_WithValidJwt_ReturnsOk")]
    public async Task ProtectedEndpoint_WithValidJwt_ReturnsOk()
    {
        var accessToken = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact(DisplayName = "AdminEndpoint_WithBuyerToken_ReturnsForbidden")]
    public async Task AdminEndpoint_WithBuyerToken_ReturnsForbidden()
    {
        var accessToken = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/test/admin");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact(DisplayName = "BuyerEndpoint_WithBuyerToken_ReturnsOk")]
    public async Task BuyerEndpoint_WithBuyerToken_ReturnsOk()
    {
        var accessToken = await GetAccessTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/test/buyer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact(DisplayName = "RoleEndpoint_WithoutJwt_ReturnsUnauthorized")]
    public async Task RoleEndpoint_WithoutJwt_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/test/buyer");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "RoleEndpoint_WithRequiredStoredRole_ReturnsOk")]
    [InlineData("Admin", "/api/test/admin")]
    [InlineData("Staff", "/api/test/staff")]
    [InlineData("Manager", "/api/test/staff")]
    [InlineData("Rider", "/api/test/rider")]
    public async Task RoleEndpoint_WithRequiredStoredRole_ReturnsOk(string role, string endpoint)
    {
        var accessToken = await GetAccessTokenForRoleAsync(role);
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Refresh Token Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Refresh_WithValidToken_ReturnsNewTokens")]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        // Register and login
        var reg = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", reg);
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = reg.Email, Password = reg.Password });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);

        // Refresh
        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = loginBody!.RefreshToken });

        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        refreshBody!.AccessToken.Should().NotBeNullOrWhiteSpace();
        refreshBody.RefreshToken.Should().NotBe(loginBody.RefreshToken,
            because: "refresh token rotation should produce a new token");
    }

    [Fact(DisplayName = "Refresh_WithExpiredOrRevokedToken_ReturnsUnauthorized")]
    public async Task Refresh_WithExpiredOrRevokedToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = "invalid-token-that-does-not-exist" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "RefreshToken_Rotation_InvalidatesOldToken")]
    public async Task RefreshToken_Rotation_InvalidatesOldToken()
    {
        // Register and login
        var reg = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", reg);
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = reg.Email, Password = reg.Password });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        var originalRefreshToken = loginBody!.RefreshToken;

        // Rotate
        await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = originalRefreshToken });

        // Try to use the old refresh token again — should be revoked
        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = originalRefreshToken });

        reuse.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "old refresh token must be revoked after rotation");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Logout Tests
    // ──────────────────────────────────────────────────────────────────────────

    [Fact(DisplayName = "Logout_WithValidToken_RevokesToken")]
    public async Task Logout_WithValidToken_RevokesToken()
    {
        var reg = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", reg);
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = reg.Email, Password = reg.Password });
        var loginBody = await loginResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);

        // Logout
        var logoutResp = await _client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest { RefreshToken = loginBody!.RefreshToken });
        logoutResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Try to refresh — should fail
        var refreshResp = await _client.PostAsJsonAsync("/api/auth/refresh",
            new RefreshTokenRequest { RefreshToken = loginBody.RefreshToken });
        refreshResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "Logout_WithAlreadyRevokedToken_DoesNotFail")]
    public async Task Logout_WithAlreadyRevokedToken_DoesNotFail()
    {
        // Logout with unknown token — should be idempotent
        var response = await _client.PostAsJsonAsync("/api/auth/logout",
            new LogoutRequest { RefreshToken = "unknown-token" });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helper: register a user and return their access token
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string> GetAccessTokenAsync()
    {
        var reg = ValidRegisterRequest();
        await _client.PostAsJsonAsync("/api/auth/register", reg);
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = reg.Email, Password = reg.Password });
        var body = await loginResp.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        return body!.AccessToken;
    }

    private async Task<string> GetAccessTokenForRoleAsync(string role)
    {
        var email = $"{role.ToLowerInvariant()}_{Guid.NewGuid():N}@example.com";
        const string password = "TestPass1!";

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            db.ApplicationUsers.Add(new ApplicationUser
            {
                FullName = $"Test {role}",
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
                Role = role,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
        var body = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        return body!.AccessToken;
    }
}
