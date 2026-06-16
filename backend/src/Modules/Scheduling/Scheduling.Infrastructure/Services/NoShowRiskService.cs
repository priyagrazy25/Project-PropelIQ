using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Scheduling.Application.Abstractions;
using Scheduling.Domain.Entities;
using Scheduling.Domain.Enums;
using Scheduling.Infrastructure.ML;

namespace Scheduling.Infrastructure.Services;

/// <summary>
/// Service implementation for no-show risk prediction and model management.
/// Implements AIR-007 (no-show prediction) and AIR-O03 (≤15min rollback).
/// </summary>
public sealed class NoShowRiskService : INoShowRiskService
{
    private readonly NoShowPredictionEngine _predictionEngine;
    private readonly ISchedulingDbContext _dbContext;
    private readonly ILogger<NoShowRiskService> _logger;

    public NoShowRiskService(
        NoShowPredictionEngine predictionEngine,
        ISchedulingDbContext dbContext,
        ILogger<NoShowRiskService> logger)
    {
        _predictionEngine = predictionEngine;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<NoShowRiskPrediction> PredictRiskAsync(
        NoShowRiskInput request,
        CancellationToken cancellationToken = default)
    {
        var prediction = _predictionEngine.Predict(request);

        _logger.LogDebug(
            "Predicted risk score {Score} ({Level}) for appointment {AppointmentId}",
            prediction.RiskScore,
            prediction.RiskLevel,
            request.AppointmentId);

        return Task.FromResult(prediction);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppointmentRiskAssessment>> GetRiskAssessmentsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var appointments = await _dbContext.Appointments
            .Include(a => a.Slot)
                .ThenInclude(s => s!.Provider)
            .Where(a => a.AppointmentDateTime >= startDate && a.AppointmentDateTime <= endDate)
            .Where(a => a.Status == AppointmentStatus.Scheduled)
            .ToListAsync(cancellationToken);

        var assessments = new List<AppointmentRiskAssessment>();

        foreach (var appointment in appointments)
        {
            // Build input from appointment data
            var input = await BuildRiskInputAsync(appointment, cancellationToken);
            var prediction = _predictionEngine.Predict(input);

            var assessment = new AppointmentRiskAssessment(
                AppointmentId: appointment.Id,
                PatientId: appointment.PatientId,
                PatientName: "Patient", // TODO: Fetch from Identity module
                AppointmentDateTime: appointment.AppointmentDateTime,
                ProviderName: appointment.Slot?.Provider?.Name ?? "Unknown",
                RiskScore: prediction.RiskScore,
                RiskLevel: prediction.RiskLevel,
                ContributingFactors: prediction.ContributingFactors);

            assessments.Add(assessment);
        }

        _logger.LogInformation(
            "Generated {Count} risk assessments for {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}",
            assessments.Count, startDate, endDate);

        return assessments.OrderByDescending(a => a.RiskScore).ToList();
    }

    /// <inheritdoc />
    public string GetActiveModelVersion()
    {
        return _predictionEngine.GetCurrentVersion() ?? "none";
    }

    /// <inheritdoc />
    public async Task<bool> RollbackToVersionAsync(
        string version,
        CancellationToken cancellationToken = default)
    {
        var result = await _predictionEngine.RollbackAsync(version, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation("Successfully rolled back to version {Version}", version);
        }
        else
        {
            _logger.LogWarning("Rollback failed: {Message}", result.Message);
        }

        return result.Success;
    }

    /// <inheritdoc />
    public IReadOnlyList<ModelVersionInfo> GetAvailableVersions()
    {
        return _predictionEngine.GetAllVersions();
    }

    /// <inheritdoc />
    public async Task<string> RetrainModelAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting model retraining");
        
        var newVersion = await _predictionEngine.RetrainAsync(
            trainingData: null, // Use synthetic data for now
            activate: true,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Retraining completed, new version: {Version}", newVersion);
        return newVersion;
    }

    /// <inheritdoc />
    public async Task<int> CalculateAndStoreRiskAsync(
        Guid appointmentId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        // Check if already calculated
        var existing = await _dbContext.NoShowRiskScores
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, cancellationToken);
        
        if (existing is not null)
        {
            _logger.LogDebug(
                "Risk score already exists for appointment {AppointmentId}: {Score}",
                appointmentId, existing.Score);
            return existing.Score;
        }

        // Get appointment to build input
        var appointment = await _dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);

        if (appointment is null)
        {
            _logger.LogWarning("Appointment {AppointmentId} not found for risk calculation", appointmentId);
            return 50; // Default medium-risk fallback
        }

        try
        {
            // Build input and predict
            var input = await BuildRiskInputAsync(appointment, cancellationToken);
            var prediction = _predictionEngine.Predict(input);

            // Store risk score
            var riskScore = new NoShowRiskScore
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                Score = prediction.RiskScore,
                RiskFactors = string.Join("; ", prediction.ContributingFactors),
                CalculatedAt = DateTime.UtcNow
            };

            _dbContext.NoShowRiskScores.Add(riskScore);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Calculated and stored risk score {Score} ({Level}) for appointment {AppointmentId}",
                prediction.RiskScore, prediction.RiskLevel, appointmentId);

            return prediction.RiskScore;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error calculating risk for appointment {AppointmentId}, using medium-risk fallback",
                appointmentId);
            
            // Model unavailability fallback: store medium-risk (50)
            var fallbackScore = new NoShowRiskScore
            {
                AppointmentId = appointmentId,
                PatientId = patientId,
                Score = 50, // Medium-risk default
                RiskFactors = "MODEL_UNAVAILABLE",
                CalculatedAt = DateTime.UtcNow
            };

            _dbContext.NoShowRiskScores.Add(fallbackScore);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return 50;
        }
    }

    /// <summary>
    /// Builds risk input features from appointment and patient history.
    /// </summary>
    private async Task<NoShowRiskInput> BuildRiskInputAsync(
        Appointment appointment,
        CancellationToken cancellationToken)
    {
        // Get patient appointment history
        var patientHistory = await _dbContext.Appointments
            .Where(a => a.PatientId == appointment.PatientId)
            .Where(a => a.AppointmentDateTime < appointment.AppointmentDateTime)
            .ToListAsync(cancellationToken);

        var priorNoShows = patientHistory.Count(a => a.Status == AppointmentStatus.NoShow);
        var priorCancellations = patientHistory.Count(a => a.Status == AppointmentStatus.Cancelled);
        var totalPrior = patientHistory.Count;

        var lastVisit = patientHistory
            .Where(a => a.Status == AppointmentStatus.Completed)
            .OrderByDescending(a => a.AppointmentDateTime)
            .FirstOrDefault();

        var daysSinceLastVisit = lastVisit != null
            ? (int)(appointment.AppointmentDateTime - lastVisit.AppointmentDateTime).TotalDays
            : -1;

        var leadTimeDays = (int)(appointment.AppointmentDateTime - DateTime.UtcNow).TotalDays;
        if (leadTimeDays < 0) leadTimeDays = 0;

        return new NoShowRiskInput(
            PatientId: appointment.PatientId,
            AppointmentId: appointment.Id,
            AppointmentType: (int)appointment.Type,
            DayOfWeek: (int)appointment.AppointmentDateTime.DayOfWeek,
            HourOfDay: appointment.AppointmentDateTime.Hour,
            PriorNoShows: priorNoShows,
            PriorCancellations: priorCancellations,
            TotalPriorAppointments: totalPrior,
            DaysSinceLastVisit: daysSinceLastVisit,
            HasInsurance: true, // TODO: Get from patient record
            IsNewPatient: totalPrior == 0,
            LeadTimeDays: leadTimeDays);
    }
}
