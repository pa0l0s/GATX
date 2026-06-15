using Gatx.Application.Common.Interfaces;
using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<AssemblyLine> AssemblyLines => Set<AssemblyLine>();
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<AssemblyLineWorkstation> AssemblyLineWorkstations => Set<AssemblyLineWorkstation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
