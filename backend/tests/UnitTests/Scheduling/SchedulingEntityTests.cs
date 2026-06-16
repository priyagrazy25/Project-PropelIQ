using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace UnitTests.Scheduling;

public class AppointmentEntityTests
{
    [Fact]
    public void NewAppointment_ShouldDefaultToScheduledStatus()
    {
        var appointment = new Appointment();
        Assert.Equal(AppointmentStatus.Scheduled, appointment.Status);
    }

    [Fact]
    public void NewAppointment_ShouldDefaultToInPersonType()
    {
        var appointment = new Appointment();
        Assert.Equal(AppointmentType.InPerson, appointment.Type);
    }

    [Fact]
    public void NewAppointment_ShouldNotBeSoftDeleted()
    {
        var appointment = new Appointment();
        Assert.False(appointment.IsDeleted);
    }

    [Fact]
    public void NewAppointment_ShouldHave30MinDuration()
    {
        var appointment = new Appointment();
        Assert.Equal(30, appointment.DurationMinutes);
    }
}

public class AppointmentSlotEntityTests
{
    [Fact]
    public void NewSlot_ShouldDefaultToAvailable()
    {
        var slot = new AppointmentSlot();
        Assert.Equal(SlotStatus.Available, slot.Status);
    }

    [Fact]
    public void NewSlot_ShouldHave30MinDuration()
    {
        var slot = new AppointmentSlot();
        Assert.Equal(30, slot.DurationMinutes);
    }
}

public class ProviderEntityTests
{
    [Fact]
    public void NewProvider_ShouldBeActiveByDefault()
    {
        var provider = new Provider();
        Assert.True(provider.IsActive);
    }

    [Fact]
    public void NewProvider_ShouldHaveEmptyCollections()
    {
        var provider = new Provider();
        Assert.Empty(provider.Slots);
        Assert.Empty(provider.Appointments);
    }
}

public class PreferredSlotSwapEntityTests
{
    [Fact]
    public void NewSwap_ShouldDefaultToPending()
    {
        var swap = new PreferredSlotSwap();
        Assert.Equal(SwapStatus.Pending, swap.Status);
    }
}

public class WaitlistEntityTests
{
    [Fact]
    public void NewWaitlist_ShouldDefaultToActive()
    {
        var entry = new Waitlist();
        Assert.Equal(WaitlistStatus.Active, entry.Status);
    }
}
