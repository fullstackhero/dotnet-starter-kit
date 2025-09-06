namespace FSH.Framework.Core.Storage;
public enum FileType
{
    Image,
    Document,
    Pdf
}

public class FileValidationRules
{
    public IReadOnlyList<string> AllowedExtensions { get; init; } = Array.Empty<string>();
    public int MaxSizeInMB { get; init; } = 5;
}

public static class FileTypeMetadata
{
    public static FileValidationRules GetRules(FileType type) =>
        type switch
        {
            FileType.Image => new() { AllowedExtensions = [".jpg", ".jpeg", ".png"], MaxSizeInMB = 5 },
            FileType.Pdf => new() { AllowedExtensions = [".pdf"], MaxSizeInMB = 10 },
            _ => throw new NotSupportedException($"Unsupported file type: {type}")
        };
}