using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.Identity;
public class PersonalDetailsModel : AuditableEntity, IAggregateRoot
{
    public Guid UserId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? AliasName { get; set; }
    public string? Role { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? Fax { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zipcode { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? CountryLocale { get; set; }
    public string? TimeZone { get; set; }
    public int IsActive { get; set; }
   // public string CompanyId { get; set; }
    public string? SecurityQuestion { get; set; }
    public string? SecurityAnswer { get; set; }
    public string? Profile { get; set; }
    public string? Designation { get; set; }

    public PersonalDetailsModel(Guid userId, string? firstName, string? lastName, string? aliasName, string? role, string? email, string? website, string? phone, string? mobile, string? fax, DateOnly? dateOfBirth, string? street, string? city, string? state, string? zipcode, string? country, string? language, string? countryLocale, string? timeZone, int isActive, string? securityQuestion, string? securityAnswer, string? profile, string? designation)
    {
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        AliasName = aliasName;
        Role = role;
        Email = email;
        Website = website;
        Phone = phone;
        Mobile = mobile;
        Fax = fax;
        DateOfBirth = dateOfBirth;
        Street = street;
        City = city;
        State = state;
        Zipcode = zipcode;
        Country = country;
        Language = language;
        CountryLocale = countryLocale;
        TimeZone = timeZone;
        IsActive = isActive;
        SecurityQuestion = securityQuestion;
        SecurityAnswer = securityAnswer;
        Profile = profile;
        Designation = designation;
    }

    public PersonalDetailsModel Update(Guid userId, string? firstName, string? lastName, string? aliasName, string? role, string? email, string? website, string? phone, string? mobile, string? fax, DateOnly? dateOfBirth, string? street, string? city, string? state, string? zipcode, string? country, string? language, string? countryLocale, string? timeZone, int isActive, string? securityQuestion, string? securityAnswer, string? profile, string? designation)
    {
        if (userId != Guid.Empty && !UserId.Equals(userId)) UserId = userId;
        if (firstName is not null && FirstName?.Equals(firstName) is not true) FirstName = firstName;
        if (lastName is not null && LastName?.Equals(lastName) is not true) LastName = lastName;
        if (aliasName is not null && AliasName?.Equals(aliasName) is not true) AliasName = aliasName;
        if (role is not null && Role?.Equals(role) is not true) Role = role;
        if (email is not null && Email?.Equals(email) is not true) Email = email;
        if (website is not null && Website?.Equals(website) is not true) Website = website;
        if (phone is not null && Phone?.Equals(phone) is not true) Phone = phone;
        if (mobile is not null && Mobile?.Equals(mobile) is not true) Mobile = mobile;
        if (DateOfBirth != dateOfBirth) DateOfBirth = dateOfBirth;
        if (fax is not null && Fax?.Equals(fax) is not true) Fax = fax;
        if (city is not null && City?.Equals(city) is not true) City = city;
        if (state is not null && State?.Equals(state) is not true) State = state;
        if (zipcode is not null && Zipcode?.Equals(zipcode) is not true) Zipcode = zipcode;
        if (country is not null && Country?.Equals(country) is not true) Country = country;
        if (language is not null && Language?.Equals(language) is not true) Language = language;
        if (countryLocale is not null && CountryLocale?.Equals(countryLocale) is not true) CountryLocale = countryLocale;
        if (timeZone is not null && TimeZone?.Equals(timeZone) is not true) TimeZone = timeZone;
        if (IsActive != isActive) IsActive = isActive;
        if (securityQuestion is not null && SecurityQuestion?.Equals(securityQuestion) is not true) SecurityQuestion = securityQuestion;
        if (securityAnswer is not null && SecurityAnswer?.Equals(securityAnswer) is not true) SecurityAnswer = securityAnswer;
        if (profile is not null && Profile?.Equals(profile) is not true) Profile = profile;
        if (designation is not null && Designation?.Equals(designation) is not true) Designation = designation;
        return this;
    }
}
