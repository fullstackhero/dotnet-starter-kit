using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.LeadData;
public class AccountDetailsModel : AuditableEntity, IAggregateRoot
{
    public string? AccountImage { get; set; }
    public Guid UserId { get; set; }
    public string? AccountName { get; set; }
    public string? AccountSite { get; set; }
    public Guid ParentAccountId { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountType { get; set; }
    public string? Industry { get; set; }
    public decimal? Revenue { get; set; }
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

    public AccountDetailsModel(string? accountImage, Guid userId, string? accountName, string? accountSite, Guid parentAccountId, string? accountNumber, string? accountType, string? industry, decimal? revenue, string? rating, string? phone, string? fax, string? website, string? tickerSymbol, string? ownerShip, int employees, string? sICCode, string? billingStreet, string? shippingStreet, string? billingCity, string? shippingCity, string? billingCode, string? shippingCode, string? billingCountry, string? shippingCountry, string? description, string? billingState, string? shippingState)
    {
        AccountImage = accountImage;
        UserId = userId;
        AccountName = accountName;
        AccountSite = accountSite;
        ParentAccountId = parentAccountId;
        AccountNumber = accountNumber;
        AccountType = accountType;
        Industry = industry;
        Revenue = revenue;
        Rating = rating;
        Phone = phone;
        Fax = fax;
        Website = website;
        TickerSymbol = tickerSymbol;
        OwnerShip = ownerShip;
        Employees = employees;
        SICCode = sICCode;
        BillingStreet = billingStreet;
        ShippingStreet = shippingStreet;
        BillingCity = billingCity;
        ShippingCity = shippingCity;
        BillingCode = billingCode;
        ShippingCode = shippingCode;
        BillingCountry = billingCountry;
        ShippingCountry = shippingCountry;
        Description = description;
        BillingState = billingState;
        ShippingState = shippingState;
    }

    public AccountDetailsModel Update(string? accountImage, Guid userId, string? accountName, string? accountSite, Guid parentAccountId, string? accountNumber, string? accountType, string? industry, decimal revenue, string? rating, string? phone, string? fax, string? website, string? tickerSymbol, string? ownerShip, int employees, string? sICCode, string? billingStreet, string? shippingStreet, string? billingCity, string? shippingCity, string? billingCode, string? shippingCode, string? billingCountry, string? shippingCountry, string? description, string? billingState, string? shippingState)
    {
        if (accountImage is not null && AccountImage?.Equals(accountImage) is not true) AccountImage = accountImage;
        if (userId != Guid.Empty && !UserId.Equals(userId)) UserId = userId;
        if (accountName is not null && AccountName?.Equals(accountName) is not true) AccountName = accountName;
        if (accountSite is not null && AccountSite?.Equals(accountSite) is not true) AccountSite = accountSite;
        if (parentAccountId != Guid.Empty && !ParentAccountId.Equals(parentAccountId)) ParentAccountId = parentAccountId;
        if (accountNumber is not null && AccountNumber?.Equals(accountNumber) is not true) AccountNumber = accountNumber;
        if (accountType is not null && AccountType?.Equals(accountType) is not true) AccountType = accountType;
        if (industry is not null && Industry?.Equals(industry) is not true) Industry = industry;
        if (Revenue != revenue) Revenue = revenue;
        if (rating is not null && Rating?.Equals(rating) is not true) Rating = rating;
        if (phone is not null && Phone?.Equals(phone) is not true) Phone = phone;
        if (fax is not null && Fax?.Equals(fax) is not true) Fax = fax;
        if (website is not null && Website?.Equals(website) is not true) Website = website;
        if (tickerSymbol is not null && TickerSymbol?.Equals(tickerSymbol) is not true) TickerSymbol = tickerSymbol;
        if (ownerShip is not null && OwnerShip?.Equals(ownerShip) is not true) OwnerShip = ownerShip;
        if (Employees != employees) Employees = employees;
        if (sICCode is not null && SICCode?.Equals(sICCode) is not true) SICCode = sICCode;
        if (billingStreet is not null && BillingStreet?.Equals(billingStreet) is not true) BillingStreet = billingStreet;
        if (shippingStreet is not null && ShippingStreet?.Equals(shippingStreet) is not true) ShippingStreet = shippingStreet;
        if (billingCity is not null && BillingCity?.Equals(billingCity) is not true) BillingCity = billingCity;
        if (shippingCity is not null && ShippingCity?.Equals(shippingCity) is not true) ShippingCity = shippingCity;
        if (billingCode is not null && BillingCode?.Equals(billingCode) is not true) BillingCode = billingCode;
        if (shippingCode is not null && ShippingCode?.Equals(shippingCode) is not true) ShippingCode = shippingCode;
        if (billingCountry is not null && BillingCountry?.Equals(billingCountry) is not true) BillingCountry = billingCountry;
        if (shippingCountry is not null && ShippingCountry?.Equals(shippingCountry) is not true) ShippingCountry = shippingCountry;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (billingState is not null && BillingState?.Equals(billingState) is not true) BillingState = billingState;
        if (shippingState is not null && ShippingState?.Equals(shippingState) is not true) ShippingState = shippingState;
        return this;
    }
}
