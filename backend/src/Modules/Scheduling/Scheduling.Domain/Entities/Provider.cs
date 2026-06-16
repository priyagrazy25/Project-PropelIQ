using SharedKernel.Domain;

namespace Scheduling.Domain.Entities;

public sealed class Provider : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<AppointmentSlot> Slots { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
