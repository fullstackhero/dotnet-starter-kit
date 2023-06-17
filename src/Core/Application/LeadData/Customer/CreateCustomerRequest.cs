using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Customer;
public class CreateCustomerRequest : IRequest<DefaultIdType>
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
    public string Insured5Name { get; set; }
    public DateTime? Insured5DOB { get; set; }
    public decimal? CommissionReceivable { get; set; }
    public decimal? CommissionPayable { get; set; }
}

public class CreateCustomerRequestHandler : IRequestHandler<CreateCustomerRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<CustomerDetailsModel> _repository;

    public CreateCustomerRequestHandler(IRepositoryWithEvents<CustomerDetailsModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        var customer = new CustomerDetailsModel(request.ContactId, request.AccountId, request.InvoiceId, request.LineOfBusinessId, request.CustomerCompanyId, request.CustomerProductId, request.NumberOfLivesId, request.SaORSiORIdv, request.DeductibleSI,
            request.ModeOfPaymentId, request.GrossPremium, request.NetPremium, request.ODPremium, request.AddOnPremium, request.TPPremium, request.PremiumForCommission, request.VehicleNumber, request.NCB, request.LifePayingTerm, request.LifeTermPoilcy, request.ISPORMarketing,
            request.TeamLead, request.PolicyNumber, request.PolicyStartDate, request.PolicyExpiryDate, request.RenewalRemainderDate, request.RenewalFlag, request.PolicyStatusId, request.PolicyIssueDate, request.Insured1Name, request.Insured1DOB, request.Insured2Name, request.Insured2DOB,
            request.Insured3Name, request.Insured3DOB, request.Insured4Name, request.Insured4DOB, request.Insured5Name, request.Insured5DOB, request.CommissionReceivable, request.CommissionPayable);

        await _repository.AddAsync(customer, cancellationToken);

        return customer.Id;
    }
}
