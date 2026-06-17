using Gatx.Application.Auth.Commands;
using Gatx.Application.Common.Authentication;
using Gatx.Domain.Entities;
using Gatx.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Xunit;

namespace Gatx.Application.Tests.Auth;

public sealed class LoginHandlerTests
{
    private static readonly JwtOptions JwtSettings = new()
    {
        Secret = "test-secret-key-that-is-long-enough-for-hs256-signing",
        Issuer = "gatx",
        Audience = "gatx",
        ExpiresMinutes = 60
    };

    [Fact]
    public async Task Login_returns_token_for_valid_credentials()
    {
        await using var dbContext = TestDb.Create();
        var hasher = new PasswordHasher();
        dbContext.Users.Add(new User("admin", hasher.Hash("admin123")));
        await dbContext.SaveChangesAsync();

        var handler = new LoginCommandHandler(
            dbContext, hasher, new JwtTokenGenerator(Options.Create(JwtSettings)));
        var result = await handler.Handle(new LoginCommand("admin", "admin123"), CancellationToken.None);

        Assert.Equal("admin", result.Username);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public async Task Login_rejects_invalid_password()
    {
        await using var dbContext = TestDb.Create();
        var hasher = new PasswordHasher();
        dbContext.Users.Add(new User("admin", hasher.Hash("admin123")));
        await dbContext.SaveChangesAsync();

        var handler = new LoginCommandHandler(
            dbContext, hasher, new JwtTokenGenerator(Options.Create(JwtSettings)));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new LoginCommand("admin", "wrong"), CancellationToken.None));
    }
}
