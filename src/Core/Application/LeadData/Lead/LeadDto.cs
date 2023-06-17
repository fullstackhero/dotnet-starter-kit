using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
public class LeadDto
{
    public Guid Id { get; set; }
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
}
