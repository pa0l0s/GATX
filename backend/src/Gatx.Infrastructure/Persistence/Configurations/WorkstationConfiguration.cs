using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gatx.Infrastructure.Persistence.Configurations;

public sealed class WorkstationConfiguration : IEntityTypeConfiguration<Workstation>
{
    public void Configure(EntityTypeBuilder<Workstation> builder)
    {
        builder.ToTable("workstations");
        builder.HasKey(station => station.Id);

        builder.Property(station => station.ShortName)
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(station => station.Name)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(station => station.PcName)
            .HasMaxLength(80)
            .IsRequired();

        builder.HasIndex(station => station.ShortName).IsUnique();
    }
}
