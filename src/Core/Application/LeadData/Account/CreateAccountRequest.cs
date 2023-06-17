using FL_CRMS_ERP_WEBAPI.Application.LeadData.Lead;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
public class CreateAccountRequest : IRequest<DefaultIdType>
{
    public string? AccountImage { get; set; }
    public Guid UserId { get; set; }
    public string? AccountName { get; set; }
    public string? AccountSite { get; set; }
    public Guid ParentAccountId { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountType { get; set; }
    public string? Industry { get; set; }
    public decimal Revenue { get; set; }
    public string? Rating { get; set; }
    public string? Phone { get; set; }
    public string? Fax { get; set; }
    public string? Website { get; set; }
    public string? TickerSymbol { get; set; }
    public string? OwnerShip { get; set; }
    public int Employees { get; set; }
    public string? SICCode { get; set; }
    public string? BillingStreet { get; set; }
    public string? ShippingStreet { get; set; }
    public string? BillingCity { get; set; }
    public string? ShippingCity { get; set; }
    public string? BillingCode { get; set; }
    public string? ShippingCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? ShippingCountry { get; set; }
    public string? Description { get; set; }
    public string? BillingState { get; set; }
    public string? ShippingState { get; set; }
}

public class CreateAccountRequestHandler : IRequestHandler<CreateAccountRequest, DefaultIdType>
{
    // Add Domain Events automatically by using IRepositoryWithEvents
    private readonly IRepositoryWithEvents<AccountDetailsModel> _repository;

    public CreateAccountRequestHandler(IRepositoryWithEvents<AccountDetailsModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var account = new AccountDetailsModel(request.AccountImage, request.UserId, request.AccountName, request.AccountSite, request.ParentAccountId, request.AccountNumber, request.AccountType, request.Industry,
            request.Revenue, request.Rating, request.Phone, request.Fax, request.Website, request.TickerSymbol, request.OwnerShip, request.Employees, request.SICCode, request.BillingStreet, request.ShippingStreet, request.BillingCity,
            request.ShippingCity, request.BillingCode, request.ShippingCode, request.BillingCountry, request.ShippingCountry, request.Description, request.BillingState, request.ShippingState);

        await _repository.AddAsync(account, cancellationToken);

        return account.Id;
    }
}
