using IdentityHub.Domain.Entities;
using IdentityHub.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityHub.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Token)
            .IsRequired()
            .HasMaxLength(256);
        builder.HasIndex(b => b.Token).IsUnique();
        builder.Property(b => b.ReplacedByToken)
            .HasMaxLength(256);
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}