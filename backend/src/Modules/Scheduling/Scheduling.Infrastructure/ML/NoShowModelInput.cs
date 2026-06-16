using Microsoft.ML.Data;

namespace Scheduling.Infrastructure.ML;

/// <summary>
/// Input data class for ML.NET no-show prediction model.
/// Features based on AIR-007: patient history, appointment type, time slot, no-show patterns.
/// </summary>
public sealed class NoShowModelInput
{
    /// <summary>Appointment type (InPerson=0, Telehealth=1, FollowUp=2, Emergency=3, WalkIn=4).</summary>
    [LoadColumn(0)]
    public float AppointmentType { get; set; }

    /// <summary>Day of week (0=Sunday through 6=Saturday).</summary>
    [LoadColumn(1)]
    public float DayOfWeek { get; set; }

    /// <summary>Hour of appointment (0-23).</summary>
    [LoadColumn(2)]
    public float HourOfDay { get; set; }

    /// <summary>Number of prior no-shows by this patient.</summary>
    [LoadColumn(3)]
    public float PriorNoShows { get; set; }

    /// <summary>Number of prior cancellations by this patient.</summary>
    [LoadColumn(4)]
    public float PriorCancellations { get; set; }

    /// <summary>Total prior appointments for this patient.</summary>
    [LoadColumn(5)]
    public float TotalPriorAppointments { get; set; }

    /// <summary>Days since patient's last visit (-1 if new patient).</summary>
    [LoadColumn(6)]
    public float DaysSinceLastVisit { get; set; }

    /// <summary>Whether patient has active insurance (1=yes, 0=no).</summary>
    [LoadColumn(7)]
    public float HasInsurance { get; set; }

    /// <summary>Whether this is a new patient (1=yes, 0=no).</summary>
    [LoadColumn(8)]
    public float IsNewPatient { get; set; }

    /// <summary>Days between booking and appointment.</summary>
    [LoadColumn(9)]
    public float LeadTimeDays { get; set; }

    /// <summary>Target label: 1 if patient no-showed, 0 otherwise.</summary>
    [LoadColumn(10)]
    [ColumnName("Label")]
    public bool DidNoShow { get; set; }
}

/// <summary>
/// Output prediction from the no-show model.
/// </summary>
public sealed class NoShowModelOutput
{
    /// <summary>Predicted label (true = will no-show).</summary>
    [ColumnName("PredictedLabel")]
    public bool Prediction { get; set; }

    /// <summary>Probability of no-show (0.0 to 1.0).</summary>
    [ColumnName("Probability")]
    public float Probability { get; set; }

    /// <summary>Raw score from the model.</summary>
    [ColumnName("Score")]
    public float Score { get; set; }
}
