using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gatx.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<Product> Products { get; }
    DbSet<AssemblyLine> AssemblyLines { get; }
    DbSet<Workstation> Workstations { get; }
    DbSet<AssemblyLineWorkstation> AssemblyLineWorkstations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
