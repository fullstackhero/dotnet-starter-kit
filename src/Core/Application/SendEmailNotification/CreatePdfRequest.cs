using FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.SendEmailNotification;
public class CreatePdfRequest : IRequest<Unit>
{

    public string? PdfData { get; set; }
    public string? FileName { get; set; }

    public string? UploadFile { get; set; }
    [NotMapped]
    public UploadRequest? UploadRequest { get; set; }

}
public class CreatePdfRequestHandler : IRequestHandler<CreatePdfRequest>
{
    private readonly IFileStorageService _file;

    public CreatePdfRequestHandler(IFileStorageService file)
    {
        _file = file;
    }

    

    public Task<Unit> Handle(CreatePdfRequest request, CancellationToken cancellationToken)
    {
        //throw new NotImplementedException();
        var uploadRequest = request.UploadRequest;
        if (uploadRequest != null)
        {
            uploadRequest.FileName = $"D-{request.FileName}{uploadRequest.Extension}";
        }

        if (uploadRequest != null)
        {
            request.UploadFile = UploadAsync(uploadRequest);
        }

        // Return a completed Task
        return Task.FromResult(Unit.Value);
    }
    
    public string UploadAsync(UploadRequest request)
    {
        if (request.Data == null) return string.Empty;
        var streamData = new MemoryStream(request.Data);
        if (streamData.Length > 0)
        {
            var folder = request.UploadType.ToString();
            var folderName = Path.Combine("Files", folder);
            var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
            bool exists = System.IO.Directory.Exists(pathToSave);
            if (!exists)
                System.IO.Directory.CreateDirectory(pathToSave);
            var fileName = request.FileName.Trim('"');
            var fullPath = Path.Combine(pathToSave, fileName);
            var dbPath = Path.Combine(folderName, fileName);
            if (File.Exists(dbPath))
            {
                dbPath = NextAvailableFilename(dbPath);
                fullPath = NextAvailableFilename(fullPath);
            }
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                streamData.CopyTo(stream);
            }
            return dbPath;
        }
        else
        {
            return string.Empty;
        }
    }

    private static string numberPattern = " ({0})";



    public static string NextAvailableFilename(string path)
    {
        // Short-cut if already available
        if (!File.Exists(path))
            return path;

        // If path has extension then insert the number pattern just before the extension and return next filename
        if (Path.HasExtension(path))
            return GetNextFilename(path.Insert(path.LastIndexOf(Path.GetExtension(path)), numberPattern));

        // Otherwise just append the pattern to the path and return next filename
        return GetNextFilename(path + numberPattern);
    }

    private static string GetNextFilename(string pattern)
    {
        string tmp = string.Format(pattern, 1);
        //if (tmp == pattern)
        //throw new ArgumentException("The pattern must include an index place-holder", "pattern");

        if (!File.Exists(tmp))
            return tmp; // short-circuit if no matches

        int min = 1, max = 2; // min is inclusive, max is exclusive/untested

        while (File.Exists(string.Format(pattern, max)))
        {
            min = max;
            max *= 2;
        }

        while (max != min + 1)
        {
            int pivot = (max + min) / 2;
            if (File.Exists(string.Format(pattern, pivot)))
                min = pivot;
            else
                max = pivot;
        }

        return string.Format(pattern, max);
    }
}

public class UploadRequest
{
    public string? FileName { get; set; }
    public string? Extension { get; set; }
    public UploadType? UploadType { get; set; }
    public byte[]? Data { get; set; }
}

public enum UploadType : byte
{
    [Description(@"Images\Products")]
    Product,

    [Description(@"Images\ProfilePictures")]
    ProfilePicture,

    [Description(@"Documents")]
    Document,

    [Description(@"Emails")]
    EmailPdf
}



