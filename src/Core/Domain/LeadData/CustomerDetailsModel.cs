using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.LeadData;
public class CustomerDetailsModel : AuditableEntity, IAggregateRoot
{
    public Guid ContactId { get; set; }
    public Guid AccountId { get; set; }
    public Guid InvoiceId { get; set; }
    public int? LineOfBusinessId { get; set; }
    public int? CustomerCompanyId { get; set; }
    public int? CustomerProductId { get; set; }
    public int? NumberOfLivesId { get; set; }
    public string? SaORSiORIdv { get; set; }
    public string? DeductibleSI { get; set; }
    public int? ModeOfPaymentId { get; set; }
    public string? GrossPremium { get; set; }
    public string? NetPremium { get; set; }
    public string? ODPremium { get; set; }
    public string? AddOnPremium { get; set; }
    public string? TPPremium { get; set; }
    public decimal? PremiumForCommission { get; set; }
    public string? VehicleNumber { get; set; }
    public string? NCB { get; set; }
    public string? LifePayingTerm { get; set; }
    public string? LifeTermPoilcy { get; set; }
    public string? ISPORMarketing { get; set; }
    public string? TeamLead { get; set; }
    public string? PolicyNumber { get; set; }
    public DateTime? PolicyStartDate { get; set; }
    public DateTime? PolicyExpiryDate { get; set; }
    public DateTime? RenewalRemainderDate { get; set; }
    public string? RenewalFlag { get; set; }
    public int? PolicyStatusId { get; set; }
    public DateTime? PolicyIssueDate { get; set; }
    public string? Insured1Name { get; set; }
    public DateTime? Insured1DOB { get; set; }

    public string? Insured2Name { get; set; }
    public DateTime? Insured2DOB { get; set; }
    public string? Insured3Name { get; set; }
    public DateTime? Insured3DOB { get; set; }
    public string? Insured4Name { get; set; }
    public DateTime? Insured4DOB { get; set; }
    public string? Insured5Name { get; set; }
    public DateTime? Insured5DOB { get; set; }
    public decimal? CommissionReceivable { get; set; }
    public decimal? CommissionPayable { get; set; }

    public CustomerDetailsModel(Guid contactId, Guid accountId, Guid invoiceId, int? lineOfBusinessId, int? customerCompanyId, int? customerProductId, int? numberOfLivesId, string? saORSiORIdv, string? deductibleSI, int? modeOfPaymentId, string? grossPremium, string? netPremium, string? oDPremium, string? addOnPremium, string? tPPremium, decimal? premiumForCommission, string? vehicleNumber, string? nCB, string? lifePayingTerm, string? lifeTermPoilcy, string? iSPORMarketing, string? teamLead, string? policyNumber, DateTime? policyStartDate, DateTime? policyExpiryDate, DateTime? renewalRemainderDate, string? renewalFlag, int? policyStatusId, DateTime? policyIssueDate, string? insured1Name, DateTime? insured1DOB, string? insured2Name, DateTime? insured2DOB, string? insured3Name, DateTime? insured3DOB, string? insured4Name, DateTime? insured4DOB, string? insured5Name, DateTime? insured5DOB, decimal? commissionReceivable, decimal? commissionPayable)
    {
        ContactId = contactId;
        AccountId = accountId;
        InvoiceId = invoiceId;
        LineOfBusinessId = lineOfBusinessId;
        CustomerCompanyId = customerCompanyId;
        CustomerProductId = customerProductId;
        NumberOfLivesId = numberOfLivesId;
        SaORSiORIdv = saORSiORIdv;
        DeductibleSI = deductibleSI;
        ModeOfPaymentId = modeOfPaymentId;
        GrossPremium = grossPremium;
        NetPremium = netPremium;
        ODPremium = oDPremium;
        AddOnPremium = addOnPremium;
        TPPremium = tPPremium;
        PremiumForCommission = premiumForCommission;
        VehicleNumber = vehicleNumber;
        NCB = nCB;
        LifePayingTerm = lifePayingTerm;
        LifeTermPoilcy = lifeTermPoilcy;
        ISPORMarketing = iSPORMarketing;
        TeamLead = teamLead;
        PolicyNumber = policyNumber;
        PolicyStartDate = policyStartDate;
        PolicyExpiryDate = policyExpiryDate;
        RenewalRemainderDate = renewalRemainderDate;
        RenewalFlag = renewalFlag;
        PolicyStatusId = policyStatusId;
        PolicyIssueDate = policyIssueDate;
        Insured1Name = insured1Name;
        Insured1DOB = insured1DOB;
        Insured2Name = insured2Name;
        Insured2DOB = insured2DOB;
        Insured3Name = insured3Name;
        Insured3DOB = insured3DOB;
        Insured4Name = insured4Name;
        Insured4DOB = insured4DOB;
        Insured5Name = insured5Name;
        Insured5DOB = insured5DOB;
        CommissionReceivable = commissionReceivable;
        CommissionPayable = commissionPayable;
    }

    public CustomerDetailsModel Update(Guid contactId, Guid accountId, Guid invoiceId, int? lineOfBusinessId, int? customerCompanyId, int? customerProductId, int? numberOfLivesId, string? saORSiORIdv, string? deductibleSI, int? modeOfPaymentId, string? grossPremium, string? netPremium, string? oDPremium, string? addOnPremium, string? tPPremium, decimal? premiumForCommission, string? vehicleNumber, string? nCB, string? lifePayingTerm, string? lifeTermPoilcy, string? iSPORMarketing, string? teamLead, string? policyNumber, DateTime? policyStartDate, DateTime? policyExpiryDate, DateTime? renewalRemainderDate, string? renewalFlag, int? policyStatusId, DateTime? policyIssueDate, string? insured1Name, DateTime? insured1DOB, string? insured2Name, DateTime? insured2DOB, string? insured3Name, DateTime? insured3DOB, string? insured4Name, DateTime? insured4DOB, string insured5Name, DateTime? insured5DOB, decimal? commissionReceivable, decimal? commissionPayable)
    {
        if (contactId != Guid.Empty && !ContactId.Equals(contactId)) ContactId = contactId;
        if (accountId != Guid.Empty && !AccountId.Equals(accountId)) AccountId = accountId;
        if (invoiceId != Guid.Empty && !InvoiceId.Equals(invoiceId)) InvoiceId = invoiceId;
        if (LineOfBusinessId != lineOfBusinessId) LineOfBusinessId = lineOfBusinessId;
        if (CustomerCompanyId != customerCompanyId) CustomerCompanyId = customerCompanyId;
        if (CustomerProductId != customerProductId) CustomerProductId = customerProductId;
        if (NumberOfLivesId != numberOfLivesId) NumberOfLivesId = numberOfLivesId;
        if (saORSiORIdv is not null && SaORSiORIdv?.Equals(saORSiORIdv) is not true) SaORSiORIdv = saORSiORIdv;
        if (deductibleSI is not null && DeductibleSI?.Equals(deductibleSI) is not true) DeductibleSI = deductibleSI;
        if (ModeOfPaymentId != modeOfPaymentId) ModeOfPaymentId = modeOfPaymentId;
        if (grossPremium is not null && GrossPremium?.Equals(grossPremium) is not true) GrossPremium = grossPremium;
        if (netPremium is not null && NetPremium?.Equals(netPremium) is not true) NetPremium = netPremium;
        if (oDPremium is not null && ODPremium?.Equals(oDPremium) is not true) ODPremium = oDPremium;
        if (addOnPremium is not null && AddOnPremium?.Equals(addOnPremium) is not true) AddOnPremium = addOnPremium;
        if (tPPremium is not null && TPPremium?.Equals(tPPremium) is not true) TPPremium = tPPremium;
        if (PremiumForCommission != premiumForCommission) PremiumForCommission = premiumForCommission;
        if (vehicleNumber is not null && VehicleNumber?.Equals(vehicleNumber) is not true) VehicleNumber = vehicleNumber;
        if (nCB is not null && NCB?.Equals(nCB) is not true) NCB = nCB;
        if (lifePayingTerm is not null && LifePayingTerm?.Equals(lifePayingTerm) is not true) LifePayingTerm = lifePayingTerm;
        if (iSPORMarketing is not null && ISPORMarketing?.Equals(iSPORMarketing) is not true) ISPORMarketing = iSPORMarketing;
        if (teamLead is not null && TeamLead?.Equals(teamLead) is not true) TeamLead = teamLead;
        if (policyExpiryDate is not null && PolicyExpiryDate?.Equals(policyExpiryDate) is not true) PolicyExpiryDate = policyExpiryDate;
        if (policyNumber is not null && PolicyNumber?.Equals(policyNumber) is not true) PolicyNumber = policyNumber;
        if (PolicyStartDate != policyStartDate) PolicyStartDate = policyStartDate;
        if (RenewalRemainderDate != renewalRemainderDate) RenewalRemainderDate = renewalRemainderDate;
        if (RenewalFlag != renewalFlag) RenewalFlag = renewalFlag;
        if (PolicyStatusId != policyStatusId) PolicyStatusId = policyStatusId;
        if (RenewalRemainderDate != renewalRemainderDate) RenewalRemainderDate = renewalRemainderDate;
        if (PolicyIssueDate != policyIssueDate) PolicyIssueDate = policyIssueDate;
        if (insured1Name is not null && Insured1Name?.Equals(insured1Name) is not true) Insured1Name = insured1Name;
        if (Insured1DOB != insured1DOB) Insured1DOB = insured1DOB;
        if (insured2Name is not null && Insured2Name?.Equals(insured2Name) is not true) Insured2Name = insured2Name;
        if (Insured2DOB != insured1DOB) Insured2DOB = insured2DOB;
        if (insured3Name is not null && Insured3Name?.Equals(insured3Name) is not true) Insured3Name = insured3Name;
        if (Insured3DOB != insured1DOB) Insured3DOB = insured3DOB;
        if (insured4Name is not null && Insured4Name?.Equals(insured4Name) is not true) Insured4Name = insured4Name;
        if (Insured4DOB != insured1DOB) Insured4DOB = insured4DOB;
        if (insured5Name is not null && Insured5Name?.Equals(insured5Name) is not true) Insured5Name = insured5Name;
        if (Insured5DOB != insured5DOB) Insured5DOB = insured5DOB;
        if (CommissionPayable != commissionPayable) CommissionPayable = commissionPayable;
        if (CommissionReceivable != commissionReceivable) CommissionReceivable = commissionReceivable;
        return this;
    }
}
