using Clinical.Application.DTOs;
using FluentValidation;

namespace Clinical.Application.Validators;

/// <summary>
/// FluentValidation rules for manual intake submission (AC-3).
/// Required fields: MedicalHistory, Symptoms, Allergies, CurrentMedications.
/// </summary>
public sealed class ManualIntakeValidator : AbstractValidator<ManualIntakeRequest>
{
    public ManualIntakeValidator()
    {
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("Patient ID is required.");

        RuleFor(x => x.MedicalHistory)
            .MaximumLength(4000).WithMessage("Medical history must not exceed 4000 characters.")
            .When(x => x.MedicalHistory is not null);

        RuleFor(x => x.Symptoms)
            .MaximumLength(1000).WithMessage("Symptoms must not exceed 1000 characters.")
            .When(x => x.Symptoms is not null);

        RuleFor(x => x.Allergies)
            .MaximumLength(1000).WithMessage("Allergies must not exceed 1000 characters.")
            .When(x => x.Allergies is not null);

        RuleFor(x => x.CurrentMedications)
            .MaximumLength(2000).WithMessage("Current medications must not exceed 2000 characters.")
            .When(x => x.CurrentMedications is not null);
    }
}
