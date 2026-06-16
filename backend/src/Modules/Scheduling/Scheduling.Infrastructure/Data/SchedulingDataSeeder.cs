using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;

namespace Scheduling.Infrastructure.Data;

/// <summary>
/// Seeds sample provider and appointment-slot data for development environments.
/// Idempotent — skips if providers already exist.
/// </summary>
public static class SchedulingDataSeeder
{
    public static async Task SeedAsync(SchedulingDbContext dbContext, ILogger logger)
    {
        try
        {
            var hasProviders = await dbContext.Providers.AnyAsync();
            if (hasProviders)
            {
                return;
            }

            var providers = CreateProviders();
            dbContext.Providers.AddRange(providers);

            var slots = CreateSlots(providers);
            dbContext.AppointmentSlots.AddRange(slots);

            await dbContext.SaveChangesAsync();
            logger.LogInformation("Scheduling seed data inserted: {ProviderCount} providers, {SlotCount} slots.",
                providers.Length, slots.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed scheduling data.");
        }
    }

    private static Provider[] CreateProviders() =>
    [
        new() { Name = "Dr. Sarah Johnson",    Specialty = "Primary Care", Location = "Downtown Clinic",      IsActive = true },
        new() { Name = "Dr. Michael Chen",     Specialty = "Cardiology",   Location = "North Medical Center",  IsActive = true },
        new() { Name = "Dr. Emily Rodriguez",  Specialty = "Dermatology",  Location = "South Health Campus",   IsActive = true },
        new() { Name = "Dr. James Wilson",     Specialty = "Orthopedics",  Location = "Downtown Clinic",       IsActive = true },
        new() { Name = "Dr. Priya Patel",      Specialty = "Pediatrics",   Location = "North Medical Center",  IsActive = true },
        new() { Name = "Dr. David Kim",        Specialty = "Cardiology",   Location = "South Health Campus",   IsActive = true },
        new() { Name = "Dr. Lisa Thompson",    Specialty = "Primary Care", Location = "North Medical Center",  IsActive = true },
        new() { Name = "Dr. Robert Martinez",  Specialty = "Orthopedics",  Location = "South Health Campus",   IsActive = true },
    ];

    private static List<AppointmentSlot> CreateSlots(Provider[] providers)
    {
        var today = DateTime.UtcNow.Date;
        var slots = new List<AppointmentSlot>();

        // Per-provider daily slot templates: (hour, durationMinutes)
        var dailyTemplates = new (int hour, int duration)[][]
        {
            [(9,30), (10,30), (11,30), (14,30)],
            [(9,45), (10,45), (14,45)],
            [(10,30), (11,30), (14,30), (15,30)],
            [(9,30), (11,30), (14,30)],
            [(9,30), (10,30), (11,30), (14,30)],
            [(10,45), (14,45), (15,45)],
            [(9,30), (10,30), (14,30)],
            [(9,30), (11,30), (14,30)],
        };

        for (var i = 0; i < providers.Length; i++)
        {
            for (var dayOffset = 0; dayOffset <= 14; dayOffset++)
            {
                foreach (var (hour, duration) in dailyTemplates[i])
                {
                    slots.Add(new AppointmentSlot
                    {
                        ProviderId = providers[i].Id,
                        StartTime = today.AddDays(dayOffset).AddHours(hour),
                        DurationMinutes = duration,
                        Status = SlotStatus.Available,
                    });
                }
            }
        }

        return slots;
    }
}
