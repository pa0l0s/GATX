using Gatx.Application.Common.Interfaces;
using Gatx.Domain.Entities;
using Gatx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gatx.WebApi.Persistence;

/// <summary>
/// Creates the schema and seeds the default user and the sample data from the
/// challenge brief. Idempotent: each section only runs when its table is empty.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;

        var dbContext = provider.GetRequiredService<AppDbContext>();
        var passwordHasher = provider.GetRequiredService<IPasswordHasher>();
        var configuration = provider.GetRequiredService<IConfiguration>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await SeedUserAsync(dbContext, passwordHasher, configuration, cancellationToken);
        await SeedSampleDataAsync(dbContext, cancellationToken);
    }

    private static async Task SeedUserAsync(
        AppDbContext dbContext,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var username = configuration["Auth:DefaultUsername"] ?? "admin";
        var password = configuration["Auth:DefaultPassword"] ?? "admin123";

        dbContext.Users.Add(new User(username, passwordHasher.Hash(password)));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSampleDataAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        // Products are seeded by EF HasData in ProductConfiguration. Each remaining
        // section is guarded independently so seeding survives a partial database.

        if (!await dbContext.Workstations.AnyAsync(cancellationToken))
        {
            dbContext.Workstations.AddRange(
                new Workstation("LWD", "Laser welding", "PC-LWD-01"),
                new Workstation("MWD", "Manual welding", "PC-MWD-01"),
                new Workstation("DRV", "Drive assembly", "PC-DRV-01"),
                new Workstation("VDT", "Voltage drop test", "PC-VDT-01"),
                new Workstation("LEK", "Leakage test", "PC-LEK-01"),
                new Workstation("HVP", "HV/PD test", "PC-HVP-01"),
                new Workstation("FIN", "Final inspection", "PC-FIN-01"),
                new Workstation("FRM", "Frame assembly", "PC-FRM-01"),
                new Workstation("TST", "Testing", "PC-TST-01"),
                new Workstation("DSP", "Dispatch", "PC-DSP-01"));
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var products = await dbContext.Products.ToDictionaryAsync(product => product.Name, cancellationToken);

        if (products.Count > 0 && !await dbContext.AssemblyLines.AnyAsync(cancellationToken))
        {
            var lines = new Dictionary<string, AssemblyLine>
            {
                ["Convey line"] = new(products["8DAB"].Id, "Convey line", true),
                ["Manual line"] = new(products["8DAB"].Id, "Manual line", true),
                ["Final assembly line"] = new(products["Simosec"].Id, "Final assembly line", true),
                ["Testing line"] = new(products["NXPlus C"].Id, "Testing line", false)
            };
            dbContext.AssemblyLines.AddRange(lines.Values);
            await dbContext.SaveChangesAsync(cancellationToken);

            // A sample ordered allocation on the final assembly line.
            var workstations = await dbContext.Workstations.ToDictionaryAsync(
                workstation => workstation.Name, cancellationToken);
            var orderedStations = new[] { "Frame assembly", "Drive assembly", "Final inspection" };
            for (var index = 0; index < orderedStations.Length; index++)
            {
                dbContext.AssemblyLineWorkstations.Add(new AssemblyLineWorkstation(
                    lines["Final assembly line"].Id, workstations[orderedStations[index]].Id, index + 1));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
