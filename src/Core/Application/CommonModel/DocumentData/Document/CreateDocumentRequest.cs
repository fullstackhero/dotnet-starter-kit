using FL_CRMS_ERP_WEBAPI.Application.LeadData.Customer;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document.CreateDocumentRequestHandler;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document;
public class CreateDocumentRequest : IRequest<DefaultIdType>
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public string? URL { get; set; }
    public Guid? DocumentTypeId { get; set; }
    public virtual DocumentTypeModel? DocumentType { get; set; }
    public Guid? DocumentOwnerID { get; set; }
    public Guid? ParentID { get; set; }
    public string? RelatedTo { get; set; }
    [NotMapped]
    public UploadRequest? UploadRequest { get; set; }
    //public FileUploadRequest? Doc { get; set; }
}

public class CreateDocumentRequestHandler : IRequestHandler<CreateDocumentRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepository<DocumentModel> _repository;
    private readonly IFileStorageService _file;

    public CreateDocumentRequestHandler(IRepository<DocumentModel> repository, IFileStorageService file) =>
        (_repository, _file) = (repository, file);

    public async Task<DefaultIdType> Handle(CreateDocumentRequest request, CancellationToken cancellationToken)
    {
        var uploadRequest = request.UploadRequest;
        if (uploadRequest != null)
        {
            uploadRequest.FileName = $"D-{Guid.NewGuid()}{uploadRequest.Extension}";
        }

        if (uploadRequest != null)
        {
            request.URL = UploadAsync(uploadRequest);
        }
        ///

        var documents = new DocumentModel(request.Title, request.Description, request.IsPublic, request.URL, request.DocumentTypeId, request.DocumentType, request.DocumentOwnerID, request.ParentID, request.RelatedTo);
        
        await _repository.AddAsync(documents, cancellationToken);

        return documents.Id;
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
        Document
    }
    
    //public static string ToDescriptionString(this Enum val)
    //{
    //    var attributes = (DescriptionAttribute[])val.GetType().GetField(val.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false);

    //    return attributes?.Length > 0
    //        ? attributes[0].Description
    //        : val.ToString();
    //}
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

    //public static class EnumExtensions
    //{
    //    public static string ToDescriptionString(this Enum val)
    //    {
    //        var attributes = (DescriptionAttribute[])val.GetType().GetField(val.ToString())?.GetCustomAttributes(typeof(DescriptionAttribute), false);

    //        return attributes?.Length > 0
    //            ? attributes[0].Description
    //            : val.ToString();
    //    }
    //}


}
