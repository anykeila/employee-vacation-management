using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VacationManagement.Domain.Entities;

namespace VacationManagement.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(t => t.Id);

        // SHA-256 rendered as 64 lowercase hex characters.
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.ExpiresAt).IsRequired();

        builder.HasOne(t => t.Employee)
            .WithMany()
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
