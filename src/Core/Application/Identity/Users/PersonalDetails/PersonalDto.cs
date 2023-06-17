using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.Identity.Users.PersonalDetails;
public class PersonalDto
{
    public Guid Id { get; set; }
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
    //public string CompanyId { get; set; }
    public string? SecurityQuestion { get; set; }
    public string? SecurityAnswer { get; set; }
    public string? Profile { get; set; }
    public string? Designation { get; set; }
}
