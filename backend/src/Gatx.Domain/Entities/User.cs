using Gatx.Domain.Common;

namespace Gatx.Domain.Entities;

public sealed class User : Entity
{
    private User()
    {
        Username = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string username, string passwordHash)
    {
        var normalized = username.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        Username = normalized;
        PasswordHash = passwordHash;
    }

    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
}
