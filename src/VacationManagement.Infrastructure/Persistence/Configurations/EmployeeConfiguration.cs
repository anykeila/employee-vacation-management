using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VacationManagement.Domain.Entities;
using VacationManagement.Domain.Enums;

namespace VacationManagement.Infrastructure.Persistence.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(e => e.Email).IsUnique();

        builder.Property(e => e.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);

        builder.HasOne(e => e.Manager)
            .WithMany(e => e.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Reference data from the exercise. Emails normalized to lowercase.
        // PasswordHash is set in the authentication commit.
        builder.HasData(
            new Employee { Id = 1, Name = "Ana Silva", Email = "ana.silva@workflow.com", Role = Role.Administrator, PasswordHash = "" },
            new Employee { Id = 2, Name = "João Pereira", Email = "joao.pereira@workflow.com", Role = Role.Manager, PasswordHash = "" },
            new Employee { Id = 5, Name = "Joana Soares", Email = "joana.soares@workflow.com", Role = Role.Manager, PasswordHash = "" },
            new Employee { Id = 3, Name = "Marta Fernandes", Email = "marta.fernandes@workflow.com", Role = Role.Employee, ManagerId = 2, PasswordHash = "" },
            new Employee { Id = 4, Name = "Henrique Martins", Email = "henrique.martins@workflow.com", Role = Role.Employee, ManagerId = 5, PasswordHash = "" });
    }
}
