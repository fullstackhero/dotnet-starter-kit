using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FL_CRMS_ERP_WEBAPI.Domain.LeadData;
public class LeadDetailsModel : AuditableEntity, IAggregateRoot
{
   // public string LeadOwner { get; set; }
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

    public LeadDetailsModel(Guid userId, string? companyName, string? firstName, string? lastName, string? title, string? email, string? phone, string? fax, string? mobile, string? website, string? leadSource, string? leadStatus, string? industry, int? noEmployess, decimal? annualRevenue, string? rating, string? skypeId, string? secondEmail, string? twitter, string? street, string? city, string? state, string? zipCode, string? country, string? description, string? leadImage, bool emailOptOut, Guid? convertedAccountId, Guid? convertedContactId, DateTime? dateOfBirth)
    {

        UserId = userId;
        CompanyName = companyName;
        FirstName = firstName;
        LastName = lastName;
        Title = title;
        Email = email;
        Phone = phone;
        Fax = fax;
        Mobile = mobile;
        Website = website;
        LeadSource = leadSource;
        LeadStatus = leadStatus;
        Industry = industry;
        NoEmployess = noEmployess;
        AnnualRevenue = annualRevenue;
        Rating = rating;
        SkypeId = skypeId;
        SecondEmail = secondEmail;
        Twitter = twitter;
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
        Description = description;
        LeadImage = leadImage;
        //IsDeleted = isDeleted;
       // CompanyId = companyId;
        EmailOptOut = emailOptOut;
        ConvertedAccountId = convertedAccountId;
        ConvertedContactId = convertedContactId;
        DateOfBirth = dateOfBirth;
    }

    public LeadDetailsModel Update(Guid userId, string? companyName, string? firstName, string? lastName, string? title, string? email, string? phone, string? fax, string? mobile, string? website, string? leadSource, string? leadStatus, string? industry, int? noEmployess, decimal? annualRevenue, string? rating, string? skypeId, string? secondEmail, string? twitter, string? street, string? city, string? state, string? zipCode, string? country, string? description, string? leadImage, bool emailOptOut, Guid? convertedAccountId, Guid? convertedContactId, DateTime? dateOfBirth)
    {
        if (userId != Guid.Empty && !UserId.Equals(userId)) UserId = userId;
        if (companyName is not null && CompanyName?.Equals(companyName) is not true) CompanyName = companyName;
        if (firstName is not null && FirstName?.Equals(firstName) is not true) FirstName = firstName;
        if (lastName is not null && LastName?.Equals(lastName) is not true) LastName = lastName;
        if (title is not null && Title?.Equals(title) is not true) Title = title;
        if (email is not null && Email?.Equals(email) is not true) Email = email;
        if (phone is not null && Phone?.Equals(phone) is not true) Phone = phone;
        if (fax is not null && Fax?.Equals(fax) is not true) Fax = fax;
        if (mobile is not null && Mobile?.Equals(mobile) is not true) Mobile = mobile;
        if (website is not null && Website?.Equals(website) is not true) Website = website;
        if (leadSource is not null && LeadSource?.Equals(leadSource) is not true) LeadSource = leadSource;
        if (leadStatus is not null && LeadStatus?.Equals(LeadStatus) is not true) LeadStatus = leadStatus;
        if (industry is not null && Industry?.Equals(industry) is not true) Industry = industry;
        if (noEmployess is not null && NoEmployess?.Equals(noEmployess) is not true) NoEmployess = noEmployess;
        if (annualRevenue is not null && AnnualRevenue?.Equals(annualRevenue) is not true) AnnualRevenue = annualRevenue;
        if (rating is not null && Rating?.Equals(rating) is not true) Rating = rating;
        if (skypeId is not null && SkypeId?.Equals(skypeId) is not true) SkypeId = skypeId;
        if (secondEmail is not null && SecondEmail?.Equals(secondEmail) is not true) SecondEmail = secondEmail;
        if (twitter is not null && Twitter?.Equals(twitter) is not true) Twitter = twitter;
        if (street is not null && Street?.Equals(street) is not true) Street = street;
        if (city is not null && City?.Equals(city) is not true) City = city;
        if (state is not null && State?.Equals(state) is not true) State = state;
        if (zipCode is not null && ZipCode?.Equals(zipCode) is not true) ZipCode = zipCode;
        if (country is not null && Country?.Equals(country) is not true) Country = country;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (leadImage is not null && LeadImage?.Equals(leadImage) is not true) LeadImage = leadImage;
        //if (companyId is not null && CompanyId?.Equals(companyId) is not true) CompanyId = companyId;
        if (EmailOptOut != emailOptOut) EmailOptOut = emailOptOut;
        if (convertedAccountId != Guid.Empty && !ConvertedAccountId.Equals(convertedAccountId)) ConvertedAccountId = convertedAccountId;
        if (convertedContactId != Guid.Empty && !ConvertedContactId.Equals(convertedContactId)) ConvertedContactId = convertedContactId;
        if (dateOfBirth is not null && DateOfBirth?.Equals(dateOfBirth) is not true) DateOfBirth = dateOfBirth;
        return this;
    }
}
