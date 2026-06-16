namespace Scheduling.Application.Abstractions;

/// <summary>
/// Service for no-show risk prediction and model management (AIR-007, TR-009).
/// </summary>
public interface INoShowRiskService
{
    /// <summary>
    /// Predicts no-show risk score (0-100) for an appointment.
    /// </summary>
    /// <param name="request">Prediction input features.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Risk score and contributing factors.</returns>
    Task<NoShowRiskPrediction> PredictRiskAsync(
        NoShowRiskInput request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets risk predictions for all appointments in a date range.
    /// </summary>
    /// <param name="startDate">Start date.</param>
    /// <param name="endDate">End date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of appointment risk assessments.</returns>
    Task<IReadOnlyList<AppointmentRiskAssessment>> GetRiskAssessmentsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active model version.
    /// </summary>
    string GetActiveModelVersion();

    /// <summary>
    /// Rolls back to a previous model version (AIR-O03: ≤15 minutes).
    /// </summary>
    /// <param name="version">Target version to roll back to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success status.</returns>
    Task<bool> RollbackToVersionAsync(string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available model versions for rollback.
    /// </summary>
    IReadOnlyList<ModelVersionInfo> GetAvailableVersions();

    /// <summary>
    /// Triggers model retraining on accumulated real data.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New model version.</returns>
    Task<string> RetrainModelAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates risk score for an appointment and stores in database.
    /// Called on booking confirmation per AC-1.
    /// </summary>
    /// <param name="appointmentId">Appointment ID.</param>
    /// <param name="patientId">Patient ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Calculated risk score (0-100).</returns>
    Task<int> CalculateAndStoreRiskAsync(
        Guid appointmentId,
        Guid patientId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Input features for no-show risk prediction.
/// </summary>
public record NoShowRiskInput(
    Guid PatientId,
    Guid AppointmentId,
    int AppointmentType,
    int DayOfWeek,
    int HourOfDay,
    int PriorNoShows,
    int PriorCancellations,
    int TotalPriorAppointments,
    int DaysSinceLastVisit,
    bool HasInsurance,
    bool IsNewPatient,
    int LeadTimeDays);

/// <summary>
/// Result of no-show risk prediction.
/// </summary>
public record NoShowRiskPrediction(
    int RiskScore,
    string RiskLevel,
    IReadOnlyList<string> ContributingFactors,
    float Probability,
    string ModelVersion);

/// <summary>
/// Appointment with computed risk assessment.
/// </summary>
public record AppointmentRiskAssessment(
    Guid AppointmentId,
    Guid PatientId,
    string PatientName,
    DateTime AppointmentDateTime,
    string ProviderName,
    int RiskScore,
    string RiskLevel,
    IReadOnlyList<string> ContributingFactors);

/// <summary>
/// Model version metadata.
/// </summary>
public record ModelVersionInfo(
    string Version,
    DateTime TrainedAt,
    int TrainingSamples,
    double Accuracy,
    bool IsActive);
