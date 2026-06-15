using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gatx.Infrastructure.Persistence.Configurations;

public sealed class AssemblyLineWorkstationConfiguration : IEntityTypeConfiguration<AssemblyLineWorkstation>
{
    public void Configure(EntityTypeBuilder<AssemblyLineWorkstation> builder)
    {
        builder.ToTable("assembly_line_workstations");
        builder.HasKey(allocation => new { allocation.AssemblyLineId, allocation.WorkstationId });

        builder.Property(allocation => allocation.Position).IsRequired();

        builder.HasIndex(allocation => new { allocation.AssemblyLineId, allocation.Position }).IsUnique();

        builder.HasOne(allocation => allocation.AssemblyLine)
            .WithMany(line => line.Workstations)
            .HasForeignKey(allocation => allocation.AssemblyLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(allocation => allocation.Workstation)
            .WithMany(station => station.AssemblyLines)
            .HasForeignKey(allocation => allocation.WorkstationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
