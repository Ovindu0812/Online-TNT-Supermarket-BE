using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TNT.UserService.Api.Data;
using TNT.UserService.Api.DTOs;
using TNT.UserService.Api.Services;

namespace TNT.UserService.Tests;

/// <summary>
/// Unit tests for UserProfileService using an in-memory database.
/// </summary>
public class UserProfileServiceTests
{
    private static UserDbContext CreateDbContext()
    {
        var opts = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase($"UserTestDb_{Guid.NewGuid():N}")
            .Options;
        return new UserDbContext(opts);
    }

    [Fact(DisplayName = "GetProfile_WhenProfileExists_ReturnsProfile")]
    public async Task GetProfile_WhenProfileExists_ReturnsProfile()
    {
        var db = CreateDbContext();
        var service = new UserProfileService(db, NullLogger<UserProfileService>.Instance);

        var userId = Guid.NewGuid();
        await service.UpsertProfileAsync(userId, new UpdateProfileRequest
        {
            DisplayName = "John Doe",
            City = "Colombo"
        });

        var result = await service.GetProfileAsync(userId);

        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("John Doe");
        result.City.Should().Be("Colombo");
        result.IdentityUserId.Should().Be(userId);
    }

    [Fact(DisplayName = "GetProfile_WhenProfileDoesNotExist_ReturnsNull")]
    public async Task GetProfile_WhenProfileDoesNotExist_ReturnsNull()
    {
        var db = CreateDbContext();
        var service = new UserProfileService(db, NullLogger<UserProfileService>.Instance);

        var result = await service.GetProfileAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact(DisplayName = "UpsertProfile_WhenProfileDoesNotExist_CreatesProfile")]
    public async Task UpsertProfile_WhenProfileDoesNotExist_CreatesProfile()
    {
        var db = CreateDbContext();
        var service = new UserProfileService(db, NullLogger<UserProfileService>.Instance);

        var userId = Guid.NewGuid();
        var result = await service.UpsertProfileAsync(userId, new UpdateProfileRequest
        {
            DisplayName = "New User",
            City = "Kandy"
        });

        result.Should().NotBeNull();
        result.IdentityUserId.Should().Be(userId);
        result.DisplayName.Should().Be("New User");
        result.City.Should().Be("Kandy");
    }

    [Fact(DisplayName = "UpsertProfile_WhenProfileExists_UpdatesProfile")]
    public async Task UpsertProfile_WhenProfileExists_UpdatesProfile()
    {
        var db = CreateDbContext();
        var service = new UserProfileService(db, NullLogger<UserProfileService>.Instance);

        var userId = Guid.NewGuid();
        await service.UpsertProfileAsync(userId, new UpdateProfileRequest
        {
            DisplayName = "Original Name",
            City = "Galle"
        });

        var updated = await service.UpsertProfileAsync(userId, new UpdateProfileRequest
        {
            DisplayName = "Updated Name",
            City = "Colombo"
        });

        updated.DisplayName.Should().Be("Updated Name");
        updated.City.Should().Be("Colombo");
        updated.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact(DisplayName = "GetProfile_DoesNotReturnAnotherUsersProfile")]
    public async Task GetProfile_DoesNotReturnAnotherUsersProfile()
    {
        var db = CreateDbContext();
        var service = new UserProfileService(db, NullLogger<UserProfileService>.Instance);

        var userAId = Guid.NewGuid();
        var userBId = Guid.NewGuid();

        await service.UpsertProfileAsync(userAId, new UpdateProfileRequest { DisplayName = "User A" });
        await service.UpsertProfileAsync(userBId, new UpdateProfileRequest { DisplayName = "User B" });

        var profileForA = await service.GetProfileAsync(userAId);
        var profileForB = await service.GetProfileAsync(userBId);

        // Each user gets their own profile
        profileForA!.DisplayName.Should().Be("User A");
        profileForB!.DisplayName.Should().Be("User B");
        profileForA.IdentityUserId.Should().NotBe(profileForB.IdentityUserId);
    }
}
