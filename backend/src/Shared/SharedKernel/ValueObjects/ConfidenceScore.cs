namespace SharedKernel.ValueObjects;

/// <summary>
/// Value object enforcing 0.0-1.0 range for confidence scores per DR-010.
/// Values below 0.7 are flagged as low confidence.
/// </summary>
public sealed record ConfidenceScore
{
    public const double LowConfidenceThreshold = 0.7;

    public double Value { get; }
    public bool IsLowConfidence => Value < LowConfidenceThreshold;

    public ConfidenceScore(double value)
    {
        if (value is < 0.0 or > 1.0)
            throw new ArgumentOutOfRangeException(nameof(value), value,
                "Confidence score must be between 0.0 and 1.0.");

        Value = value;
    }

    public static implicit operator double(ConfidenceScore score) => score.Value;
}
