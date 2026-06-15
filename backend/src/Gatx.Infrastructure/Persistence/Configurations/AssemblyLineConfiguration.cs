using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gatx.Infrastructure.Persistence.Configurations;

public sealed class AssemblyLineConfiguration : IEntityTypeConfiguration<AssemblyLine>
{
    public void Configure(EntityTypeBuilder<AssemblyLine> builder)
    {
        builder.ToTable("assembly_lines");
        builder.HasKey(line => line.Id);

        builder.Property(line => line.Name)
            .HasMaxLength(160)
            .IsRequired();

        builder.HasOne(line => line.Product)
            .WithMany(product => product.AssemblyLines)
            .HasForeignKey(line => line.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(line => new { line.ProductId, line.Name }).IsUnique();
    }
}
