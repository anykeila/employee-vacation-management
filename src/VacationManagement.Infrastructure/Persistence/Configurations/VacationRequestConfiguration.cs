using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VacationManagement.Domain.Entities;
using VacationManagement.Domain.Enums;

namespace VacationManagement.Infrastructure.Persistence.Configurations;

public class VacationRequestConfiguration : IEntityTypeConfiguration<VacationRequest>
{
    private static readonly DateTimeOffset SeedTimestamp = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<VacationRequest> builder)
    {
        builder.HasKey(v => v.Id);

        builder.Property(v => v.StartDate).IsRequired();
        builder.Property(v => v.EndDate).IsRequired();
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(v => v.Notes).HasMaxLength(1000);
        builder.Property(v => v.CreatedAt).IsRequired();

        builder.Ignore(v => v.Period);

        builder.HasOne(v => v.Employee)
            .WithMany(e => e.VacationRequests)
            .HasForeignKey(v => v.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Supports the overlap query: WHERE Status = 'Approved' AND StartDate <= @end AND EndDate >= @start.
        builder.HasIndex(v => new { v.Status, v.StartDate, v.EndDate });

        // Historical approved vacations from the exercise. Seeded directly so they
        // bypass the "no past start date" rule that applies to new requests (see README).
        builder.HasData(
            new VacationRequest { Id = 1, EmployeeId = 3, StartDate = new DateOnly(2025, 8, 1), EndDate = new DateOnly(2025, 8, 5), Status = VacationStatus.Approved, Notes = "Primeiras férias do ano", CreatedAt = SeedTimestamp },
            new VacationRequest { Id = 2, EmployeeId = 4, StartDate = new DateOnly(2025, 8, 10), EndDate = new DateOnly(2025, 8, 15), Status = VacationStatus.Approved, Notes = "Viagem em família", CreatedAt = SeedTimestamp },
            new VacationRequest { Id = 3, EmployeeId = 2, StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2025, 9, 12), Status = VacationStatus.Approved, Notes = "Primeiras férias do ano", CreatedAt = SeedTimestamp });
    }
}
