using Clinical.Application.Abstractions;
using Clinical.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Scheduling.Domain.Entities;
using Scheduling.Infrastructure.Data;

namespace Host.Services;

/// <summary>
/// Cross-module manual intake persistence bridging Clinical and Scheduling modules.
/// </summary>
public sealed class ManualIntakePersistenceService : IManualIntakePersistence
{
    private readonly SchedulingDbContext _schedulingDb;

    public ManualIntakePersistenceService(SchedulingDbContext schedulingDb)
    {
        _schedulingDb = schedulingDb;
    }

    public async Task<ManualIntakeResponse?> GetByAppointmentIdAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _schedulingDb.IntakeRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, cancellationToken);

        return record is null ? null : MapToResponse(record);
    }

    public async Task<ManualIntakeResponse> UpsertDraftAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        // When appointmentId is Guid.Empty the intake has no appointment yet.
        // Look up by PatientId instead and store AppointmentId as null.
        var hasAppointment = appointmentId != Guid.Empty;

        var record = hasAppointment
            ? await _schedulingDb.IntakeRecords
                .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, cancellationToken)
            : await _schedulingDb.IntakeRecords
                .FirstOrDefaultAsync(r => r.PatientId == request.PatientId && r.AppointmentId == null && !r.IsComplete, cancellationToken);

        if (record is null)
        {
            record = new IntakeRecord
            {
                PatientId = request.PatientId,
                AppointmentId = hasAppointment ? appointmentId : null,
                ChiefComplaint = request.Symptoms,
                CurrentMedications = request.CurrentMedications,
                Allergies = request.Allergies,
                MedicalHistory = request.MedicalHistory,
                IsComplete = false
            };
            _schedulingDb.IntakeRecords.Add(record);
        }
        else
        {
            record.ChiefComplaint = request.Symptoms;
            record.CurrentMedications = request.CurrentMedications;
            record.Allergies = request.Allergies;
            record.MedicalHistory = request.MedicalHistory;
        }

        await _schedulingDb.SaveChangesAsync(cancellationToken);
        return MapToResponse(record);
    }

    public async Task<ManualIntakeResponse> SubmitAsync(
        Guid appointmentId,
        ManualIntakeRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAppointment = appointmentId != Guid.Empty;

        var record = hasAppointment
            ? await _schedulingDb.IntakeRecords
                .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, cancellationToken)
            : await _schedulingDb.IntakeRecords
                .FirstOrDefaultAsync(r => r.PatientId == request.PatientId && r.AppointmentId == null && !r.IsComplete, cancellationToken);

        if (record is null)
        {
            record = new IntakeRecord
            {
                PatientId = request.PatientId,
                AppointmentId = hasAppointment ? appointmentId : null,
                ChiefComplaint = request.Symptoms,
                CurrentMedications = request.CurrentMedications,
                Allergies = request.Allergies,
                MedicalHistory = request.MedicalHistory,
                IsComplete = true,
                CompletedAt = DateTime.UtcNow
            };
            _schedulingDb.IntakeRecords.Add(record);
        }
        else
        {
            record.ChiefComplaint = request.Symptoms;
            record.CurrentMedications = request.CurrentMedications;
            record.Allergies = request.Allergies;
            record.MedicalHistory = request.MedicalHistory;
            record.IsComplete = true;
            record.CompletedAt = DateTime.UtcNow;
        }

        await _schedulingDb.SaveChangesAsync(cancellationToken);
        return MapToResponse(record);
    }

    public async Task<ManualIntakeResponse?> UpdateFieldAsync(
        Guid appointmentId,
        string fieldName,
        string value,
        CancellationToken cancellationToken = default)
    {
        var record = await _schedulingDb.IntakeRecords
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId, cancellationToken);

        if (record is null)
        {
            return null;
        }

        switch (fieldName.ToUpperInvariant())
        {
            case "MEDICALHISTORY":
                record.MedicalHistory = value;
                break;
            case "SYMPTOMS":
                record.ChiefComplaint = value;
                break;
            case "ALLERGIES":
                record.Allergies = value;
                break;
            case "CURRENTMEDICATIONS":
                record.CurrentMedications = value;
                break;
        }

        await _schedulingDb.SaveChangesAsync(cancellationToken);
        return MapToResponse(record);
    }

    private static ManualIntakeResponse MapToResponse(IntakeRecord record)
    {
        return new ManualIntakeResponse(
            Id: record.Id,
            PatientId: record.PatientId,
            AppointmentId: record.AppointmentId,
            MedicalHistory: record.MedicalHistory,
            Symptoms: record.ChiefComplaint,
            Allergies: record.Allergies,
            CurrentMedications: record.CurrentMedications,
            IsComplete: record.IsComplete,
            CompletedAt: record.CompletedAt,
            CreatedAt: record.CreatedAt,
            UpdatedAt: record.UpdatedAt);
    }
}
