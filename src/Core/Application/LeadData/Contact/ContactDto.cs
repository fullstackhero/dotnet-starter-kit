using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Contact;
public class ContactDto
{
    public Guid Id { get; set; }
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
    //Naveen
    public Guid CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public Guid LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }
}
