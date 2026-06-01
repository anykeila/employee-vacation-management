using VacationManagement.Domain.Enums;
using VacationManagement.Domain.ValueObjects;

namespace VacationManagement.Domain.Entities;

public class VacationRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public VacationStatus Status { get; set; } = VacationStatus.Pending;
    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateRange Period => new(StartDate, EndDate);
}
