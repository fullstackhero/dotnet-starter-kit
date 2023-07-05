using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.CommonModel.DocumentData.Document.CreateDocumentRequestHandler;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
public class CreateLeadRequest : IRequest<DefaultIdType>
{
    //public string LeadOwner { get; set; }
    public Guid UserId { get; set; }
    public string? CompanyName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Title { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Mobile { get; set; }
    public string? Website { get; set; }
    public string? LeadSource { get; set; }
    public string? LeadStatus { get; set; }
    public string? Industry { get; set; }
    public int? NoEmployess { get; set; }
    public decimal? AnnualRevenue { get; set; }
    public string? Rating { get; set; }
    public string? SkypeId { get; set; }
    public string? SecondEmail { get; set; }
    public string? Twitter { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
    public string? Description { get; set; }
    public string? LeadImage { get; set; }
    //public int? IsDeleted { get; set; }
    //public string CompanyId { get; set; }
    public bool EmailOptOut { get; set; }

    public Guid? ConvertedAccountId { get; set; }

    public Guid? ConvertedContactId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    [NotMapped]
    public UploadRequest? UploadRequest { get; set; }

}


//public class CreateLeadRequestValidator : CustomValidator<CreateLeadRequest>
//{
//    public CreateLeadRequestValidator(IReadRepository<LeadDetailsModel> repository, IStringLocalizer<CreateLeadRequestValidator> T) =>
//        RuleFor(p => p.Name)
//            .NotEmpty()
//            .MaximumLength(75)
//            .MustAsync(async (name, ct) => await repository.GetBySpecAsync(new SupplierByNameSpec(name), ct) is null)
//                .WithMessage((_, name) => T["Supplier {0} already Exists.", name]);
//}

public class CreateLeadRequestHandler : IRequestHandler<CreateLeadRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<LeadDetailsModel> _repository;

    public CreateLeadRequestHandler(IRepositoryWithEvents<LeadDetailsModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateLeadRequest request, CancellationToken cancellationToken)
    {
        var uploadRequest1 = request.UploadRequest;
        if (uploadRequest1 != null)
        {
            uploadRequest1.FileName = $"D-{Guid.NewGuid()}{uploadRequest1.Extension}";
        }

        //if (uploadRequest1 != null)
        //{
        //    request.LeadImage = UploadAsync(uploadRequest1);
        //}

        var lead = new LeadDetailsModel(request.UserId, request.CompanyName, request.FirstName, request.LastName, request.Title, request.Email,
            request.Phone, request.Fax, request.Mobile, request.Website, request.LeadSource, request.LeadStatus, request.Industry, request.NoEmployess, request.AnnualRevenue, request.Rating, request.SkypeId,
            request.SecondEmail, request.Twitter, request.Street, request.City, request.Street, request.ZipCode, request.Country, request.Description, request.LeadImage, request.EmailOptOut,
            request.ConvertedAccountId, request.ConvertedContactId, request.DateOfBirth);

        await _repository.AddAsync(lead, cancellationToken);

        return lead.Id;
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
}