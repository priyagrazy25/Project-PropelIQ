namespace Clinical.Domain.Enums;

public enum ProcessingStatus
{
    Pending = 0,
    Uploading = 1,
    OcrInProgress = 2,
    NerInProgress = 3,
    CodingInProgress = 4,
    Completed = 5,
    Failed = 6
}
