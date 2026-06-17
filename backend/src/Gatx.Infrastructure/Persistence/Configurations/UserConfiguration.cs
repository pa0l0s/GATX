using Gatx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Gatx.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username).IsRequired().HasMaxLength(120);
        builder.Property(user => user.PasswordHash).IsRequired();

        builder.HasIndex(user => user.Username).IsUnique();
    }
}
