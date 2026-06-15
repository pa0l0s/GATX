using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gatx.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(product => product.Name).IsUnique();

        builder.HasData(
            SeedProduct("11111111-1111-1111-1111-111111111111", "8DAB"),
            SeedProduct("22222222-2222-2222-2222-222222222222", "8DJH"),
            SeedProduct("33333333-3333-3333-3333-333333333333", "Simosec"),
            SeedProduct("44444444-4444-4444-4444-444444444444", "NXPlus C"));
    }

    private static Product SeedProduct(string id, string name)
    {
        var product = new Product(name);
        typeof(Product).BaseType!.GetProperty("Id")!.SetValue(product, Guid.Parse(id));
        return product;
    }
}
