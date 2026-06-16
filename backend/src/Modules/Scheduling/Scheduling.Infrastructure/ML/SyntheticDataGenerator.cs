namespace Scheduling.Infrastructure.ML;

/// <summary>
/// Generates synthetic training data for initial model training (AC-4).
/// Creates realistic no-show patterns based on domain knowledge.
/// </summary>
public static class SyntheticDataGenerator
{
    private static readonly Random _random = new(42); // Fixed seed for reproducibility

    /// <summary>
    /// Generates synthetic training samples with realistic no-show patterns.
    /// </summary>
    /// <param name="count">Number of samples to generate.</param>
    /// <returns>List of training samples.</returns>
    public static IReadOnlyList<NoShowModelInput> GenerateTrainingData(int count = 5000)
    {
        var samples = new List<NoShowModelInput>(count);

        for (int i = 0; i < count; i++)
        {
            samples.Add(GenerateSample());
        }

        return samples;
    }

    private static NoShowModelInput GenerateSample()
    {
        // Generate features
        var appointmentType = _random.Next(0, 5); // 0-4
        var dayOfWeek = _random.Next(0, 7); // 0-6
        var hourOfDay = _random.Next(8, 18); // 8 AM to 5 PM
        var priorNoShows = GenerateSkewedCount(0.7, 0, 5);
        var priorCancellations = GenerateSkewedCount(0.6, 0, 8);
        var totalPriorAppointments = priorNoShows + priorCancellations + _random.Next(0, 20);
        var daysSinceLastVisit = totalPriorAppointments > 0 ? _random.Next(1, 365) : -1;
        var hasInsurance = _random.NextDouble() > 0.15; // 85% have insurance
        var isNewPatient = totalPriorAppointments == 0;
        var leadTimeDays = _random.Next(1, 60);

        // Calculate no-show probability based on realistic factors
        var noShowProbability = CalculateNoShowProbability(
            appointmentType,
            dayOfWeek,
            hourOfDay,
            priorNoShows,
            priorCancellations,
            totalPriorAppointments,
            daysSinceLastVisit,
            hasInsurance,
            isNewPatient,
            leadTimeDays);

        var didNoShow = _random.NextDouble() < noShowProbability;

        return new NoShowModelInput
        {
            AppointmentType = appointmentType,
            DayOfWeek = dayOfWeek,
            HourOfDay = hourOfDay,
            PriorNoShows = priorNoShows,
            PriorCancellations = priorCancellations,
            TotalPriorAppointments = totalPriorAppointments,
            DaysSinceLastVisit = daysSinceLastVisit,
            HasInsurance = hasInsurance ? 1 : 0,
            IsNewPatient = isNewPatient ? 1 : 0,
            LeadTimeDays = leadTimeDays,
            DidNoShow = didNoShow
        };
    }

    private static int GenerateSkewedCount(double zeroProb, int min, int max)
    {
        if (_random.NextDouble() < zeroProb)
            return 0;
        return _random.Next(min + 1, max + 1);
    }

    private static double CalculateNoShowProbability(
        int appointmentType,
        int dayOfWeek,
        int hourOfDay,
        int priorNoShows,
        int priorCancellations,
        int totalPriorAppointments,
        int daysSinceLastVisit,
        bool hasInsurance,
        bool isNewPatient,
        int leadTimeDays)
    {
        // Base no-show rate ~10%
        double prob = 0.10;

        // Prior no-shows strongly increase risk
        prob += priorNoShows * 0.15; // Each prior no-show adds 15%

        // Prior cancellations slightly increase risk
        prob += priorCancellations * 0.03;

        // New patients have higher risk
        if (isNewPatient)
            prob += 0.08;

        // No insurance increases risk
        if (!hasInsurance)
            prob += 0.12;

        // Monday appointments have slightly higher no-show
        if (dayOfWeek == 1) // Monday
            prob += 0.05;

        // Friday afternoon appointments have higher no-show
        if (dayOfWeek == 5 && hourOfDay >= 14) // Friday afternoon
            prob += 0.07;

        // Early morning (8-9 AM) appointments have higher no-show
        if (hourOfDay < 10)
            prob += 0.04;

        // Very long lead times increase no-show risk
        if (leadTimeDays > 30)
            prob += 0.06;
        else if (leadTimeDays > 14)
            prob += 0.03;

        // Walk-ins and emergencies have lower no-show
        if (appointmentType == 4) // WalkIn
            prob -= 0.08;
        else if (appointmentType == 3) // Emergency
            prob -= 0.05;

        // Telehealth slightly lower no-show
        if (appointmentType == 1) // Telehealth
            prob -= 0.03;

        // Patients with long history and no prior no-shows are more reliable
        if (totalPriorAppointments > 5 && priorNoShows == 0)
            prob -= 0.05;

        // Long time since last visit increases risk
        if (daysSinceLastVisit > 180)
            prob += 0.04;

        // Clamp to valid probability range
        return Math.Clamp(prob, 0.01, 0.95);
    }
}
