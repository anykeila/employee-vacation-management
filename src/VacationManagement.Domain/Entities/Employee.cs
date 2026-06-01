using VacationManagement.Domain.Enums;

namespace VacationManagement.Domain.Entities;

public class Employee
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public Role Role { get; set; }
    public string PasswordHash { get; set; } = string.Empty;

    // Self-referencing Manager -> Employee relationship.
    // An employee has at most one manager; the rationale (vs. many-to-many) is in the README.
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> Subordinates { get; set; } = new List<Employee>();

    public ICollection<VacationRequest> VacationRequests { get; set; } = new List<VacationRequest>();
}
