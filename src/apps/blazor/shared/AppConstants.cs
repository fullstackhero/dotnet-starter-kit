using System.Collections.ObjectModel;

namespace FSH.Starter.Blazor.Shared;
public static class AppConstants
{
    public static readonly Collection<string> SupportedImageFormats =
    [
        ".jpeg",
        ".jpg",
        ".png"
    ];
    public static readonly string StandardImageFormat = "image/jpeg";
    public static readonly int MaxImageWidth = 1500;
    public static readonly int MaxImageHeight = 1500;
    public static readonly long MaxAllowedSize = 1000000; // Allows Max File Size of 1 Mb.
    
    public static readonly Collection<string> SupportedExcelFormats =
    [
        ".xls",
        ".xlsx"
    ];
    public static readonly string StandardExcelFormat = "excel/xlsx";
    public static readonly long MaxExcelFileSize = 20000000;

    public static readonly Collection<string> SupportedQuizMediaFormats =
    [
        ".zip"
    ];
    public static readonly string StandardQuizMediaFormat = "quizmedia/zip";
    public static readonly long MaxQuizMediaFileSize = 20000000;

    public static readonly Collection<string> SupportedDocumentFormats =
    [
        ".pdf",
        ".doc",
        ".zip",
        ".rar"
    ];
    public static readonly string StandardDocumentFormat = "document/pdf";
    public static readonly long MaxDocumentFileSize = 20000000;
}
