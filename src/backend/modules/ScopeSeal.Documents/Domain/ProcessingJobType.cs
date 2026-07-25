namespace ScopeSeal.Documents.Domain;

public enum ProcessingJobType
{
    ContentValidation = 0,
    MalwareScan = 1,
    PreviewGeneration = 2,
    TextExtraction = 3
}
