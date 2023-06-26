using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
public class CreateContactRequest : IRequest<DefaultIdType>
{
    public Guid ContactOwnerId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid AccountId { get; set; }
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? HomePhone { get; set; }
    public string? Fax { get; set; }
    public string? Mobile { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? SkypeId { get; set; }
    public string? SecondEmail { get; set; }
    public string? Twitter { get; set; }
    public string? MailingStreet { get; set; }
    public string? OtherStreet { get; set; }
    public string? MailingCity { get; set; }
    public string? OtherCity { get; set; }
    public string? MailingState { get; set; }
    public string? OtherState { get; set; }
    public string? MailingZipcode { get; set; }
    public string? OtherZipCode { get; set; }
    public string? MailingCountry { get; set; }
    public string? OtherCountry { get; set; }
    public string? Description { get; set; }
    public string? ContactImage { get; set; }
    public string? LeadSource { get; set; }
    public bool EmailOptOut { get; set; }
    public string? Title { get; set; }
    public string? Assistant { get; set; }
    public string? AssistantNumber { get; set; }
    public string? Source { get; set; }
    public Guid LeadId { get; set; }
    public Guid? ReportTo { get; set; }
    public string? OtherPhone { get; set; }
}

public class CreateContactRequestHandler : IRequestHandler<CreateContactRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<ContactDetailsModel> _repository;

    public CreateContactRequestHandler(IRepositoryWithEvents<ContactDetailsModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateContactRequest request, CancellationToken cancellationToken)
    {
        var contact = new ContactDetailsModel(request.ContactOwnerId, request.FirstName, request.LastName, request.AccountId, request.Email, request.Department, request.Phone, request.HomePhone, request.Fax,
            request.Mobile, request.DateOfBirth, request.SkypeId, request.SecondEmail, request.Twitter, request.MailingStreet, request.OtherStreet, request.MailingCity, request.OtherCity, request.MailingState,
            request.OtherState, request.MailingZipcode, request.OtherZipCode, request.MailingCountry, request.OtherCountry, request.Description, request.ContactImage, request.LeadSource, request.EmailOptOut, request.Title,
            request.Assistant, request.AssistantNumber, request.Source, request.LeadId, request.ReportTo, request.OtherPhone);

        await _repository.AddAsync(contact, cancellationToken);

        return contact.Id;
    }
}
