using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.LeadData;
public class ContactDetailsModel : AuditableEntity, IAggregateRoot
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
    public Guid? LeadId { get; set; }
    public Guid? ReportTo { get; set; }
    public string? OtherPhone { get; set; }

    public ContactDetailsModel(Guid contactOwnerId, string? firstName, string? lastName, Guid accountId, string? email, string? department, string? phone, string? homePhone, string? fax, string? mobile, DateTime? dateOfBirth, string? skypeId, string? secondEmail, string? twitter, string? mailingStreet, string? otherStreet, string? mailingCity, string? otherCity, string? mailingState, string? otherState, string? mailingZipcode, string? otherZipCode, string? mailingCountry, string? otherCountry, string? description, string? contactImage, string? leadSource, bool emailOptOut, string? title, string? assistant, string? assistantNumber, string? source, Guid? leadId, Guid? reportTo, string? otherPhone)
    {
        ContactOwnerId = contactOwnerId;
        FirstName = firstName;
        LastName = lastName;
        AccountId = accountId;
        Email = email;
        Department = department;
        Phone = phone;
        HomePhone = homePhone;
        Fax = fax;
        Mobile = mobile;
        DateOfBirth = dateOfBirth;
        SkypeId = skypeId;
        SecondEmail = secondEmail;
        Twitter = twitter;
        MailingStreet = mailingStreet;
        OtherStreet = otherStreet;
        MailingCity = mailingCity;
        OtherCity = otherCity;
        MailingState = mailingState;
        OtherState = otherState;
        MailingZipcode = mailingZipcode;
        OtherZipCode = otherZipCode;
        MailingCountry = mailingCountry;
        OtherCountry = otherCountry;
        Description = description;
        ContactImage = contactImage;
        LeadSource = leadSource;
        EmailOptOut = emailOptOut;
        Title = title;
        Assistant = assistant;
        AssistantNumber = assistantNumber;
        Source = source;
        LeadId = leadId;
        ReportTo = reportTo;
        OtherPhone = otherPhone;
    }

    public ContactDetailsModel Update(Guid contactOwnerId, string? firstName, string? lastName, Guid accountId, string? email, string? department, string? phone, string? homePhone, string? fax, string? mobile, DateTime? dateOfBirth, string? skypeId, string? secondEmail, string? twitter, string? mailingStreet, string? otherStreet, string? mailingCity, string? otherCity, string? mailingState, string? otherState, string? mailingZipcode, string? otherZipCode, string? mailingCountry, string? otherCountry, string? description, string? contactImage, string? leadSource, bool emailOptOut, string? title, string? assistant, string? assistantNumber, string? source, Guid? leadId, Guid? reportTo, string? otherPhone)
    {
        if (contactOwnerId != Guid.Empty && !ContactOwnerId.Equals(contactOwnerId)) ContactOwnerId = contactOwnerId;
        if (firstName is not null && FirstName?.Equals(firstName) is not true) FirstName = firstName;
        if (lastName is not null && LastName?.Equals(lastName) is not true) LastName = lastName;
        if (accountId != Guid.Empty && !AccountId.Equals(accountId)) AccountId = accountId;
        if (email is not null && Email?.Equals(email) is not true) Email = email;
        if (department is not null && Department?.Equals(department) is not true) Department = department;
        if (phone is not null && Phone?.Equals(phone) is not true) Phone = phone;
        if (homePhone is not null && HomePhone?.Equals(homePhone) is not true) HomePhone = homePhone;
        if (fax is not null && Fax?.Equals(fax) is not true) Fax = fax;
        if (mobile is not null && Mobile?.Equals(mobile) is not true) Mobile = mobile;
        if (DateOfBirth != dateOfBirth) DateOfBirth = dateOfBirth;
        if (skypeId is not null && SkypeId?.Equals(skypeId) is not true) SkypeId = skypeId;
        if (secondEmail is not null && SecondEmail?.Equals(secondEmail) is not true) SecondEmail = secondEmail;
        if (twitter is not null && Twitter?.Equals(twitter) is not true) Twitter = twitter;
        if (mailingStreet is not null && MailingStreet?.Equals(mailingStreet) is not true) MailingStreet = mailingStreet;
        if (otherStreet is not null && OtherStreet?.Equals(otherStreet) is not true) OtherStreet = otherStreet;
        if (mailingCity is not null && MailingCity?.Equals(mailingCity) is not true) MailingCity = mailingCity;
        if (otherCity is not null && OtherCity?.Equals(otherCity) is not true) OtherCity = otherCity;
        if (mailingState is not null && MailingState?.Equals(mailingState) is not true) MailingState = mailingState;
        if (otherState is not null && OtherState?.Equals(otherState) is not true) OtherState = otherState;
        if (mailingZipcode is not null && MailingZipcode?.Equals(mailingZipcode) is not true) MailingZipcode = mailingZipcode;
        if (otherZipCode is not null && OtherZipCode?.Equals(otherZipCode) is not true) OtherZipCode = otherZipCode;
        if (mailingCountry is not null && MailingCountry?.Equals(mailingCountry) is not true) MailingCountry = mailingCountry;
        if (otherCountry is not null && OtherCountry?.Equals(otherCountry) is not true) OtherCountry = otherCountry;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (contactImage is not null && ContactImage?.Equals(contactImage) is not true) ContactImage = contactImage;
        if (leadSource is not null && LeadSource?.Equals(leadSource) is not true) LeadSource = leadSource;
        if (EmailOptOut != emailOptOut) EmailOptOut = emailOptOut;
        if (title is not null && Title?.Equals(title) is not true) Title = title;
        if (assistant is not null && Assistant?.Equals(assistant) is not true) Assistant = assistant;
        if (assistantNumber is not null && AssistantNumber?.Equals(assistantNumber) is not true) AssistantNumber = assistantNumber;
        if (source is not null && Source?.Equals(source) is not true) Source = source;
        if (leadId != Guid.Empty && !LeadId.Equals(leadId)) LeadId = leadId;
        if (reportTo != Guid.Empty && !ReportTo.Equals(reportTo)) ReportTo = reportTo;
        if (otherPhone is not null && OtherPhone?.Equals(otherPhone) is not true) OtherPhone = otherPhone;
        return this;
    }
}
