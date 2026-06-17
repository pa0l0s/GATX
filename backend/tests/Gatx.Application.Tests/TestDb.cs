using Gatx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Tests;

internal static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
