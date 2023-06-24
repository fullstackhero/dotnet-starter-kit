using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.LeadData;
public class QuotationDetailsModel : AuditableEntity, IAggregateRoot
{
    public string? Team { get; set; }
    public Guid? DealId { get; set; }
    public string? Subject { get; set; }
    public DateTime? ValidDate { get; set; }
    public Guid ContactId { get; set; }
    public Guid AccountId { get; set; }
    public string? Carrier { get; set; }
    public string? QuoteStage { get; set; }
    public string? BillingStreet { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? ShippingStreet { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingCode { get; set; }
    public string? ShippingCountry { get; set; }
    public string? Description { get; set; }
    public string? TermsConditions { get; set; }
    public Guid QuoteOwnerId { get; set; }

    public Guid? LeadId { get; set; }

    [NotMapped]
    public List<Quote> QuoteItems { get; set; }

    [Column(TypeName = "jsonb")]
    public string QuoteItemsJson
    {
        get
        {
            return JsonConvert.SerializeObject(QuoteItems);
        }
        set
        {
            QuoteItems = JsonConvert.DeserializeObject<List<Quote>>(value);
        }
    }
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAdjustment { get; set; }
    public decimal GrandTotal { get; set; }

    public QuotationDetailsModel(string? team, Guid? dealId, string? subject, DateTime? validDate, Guid contactId, Guid accountId, string? carrier, string? quoteStage, string? billingStreet, string? billingCity, string? billingState, string? billingCode, string? billingCountry, string? shippingStreet, string? shippingCity, string? shippingState, string? shippingCode, string? shippingCountry, string? description, string? termsConditions, Guid quoteOwnerId, Guid? leadId, string quoteItemsJson, decimal subTotal, decimal totalDiscount, decimal totalTax, decimal totalAdjustment, decimal grandTotal)
    {
        Team = team;
        DealId = dealId;
        Subject = subject;
        ValidDate = validDate;
        ContactId = contactId;
        AccountId = accountId;
        Carrier = carrier;
        QuoteStage = quoteStage;
        BillingStreet = billingStreet;
        BillingCity = billingCity;
        BillingState = billingState;
        BillingCode = billingCode;
        BillingCountry = billingCountry;
        ShippingStreet = shippingStreet;
        ShippingCity = shippingCity;
        ShippingState = shippingState;
        ShippingCode = shippingCode;
        ShippingCountry = shippingCountry;
        Description = description;
        TermsConditions = termsConditions;
        QuoteOwnerId = quoteOwnerId;
        LeadId = leadId;
        QuoteItems = new List<Quote>();
        QuoteItemsJson = quoteItemsJson;
        SubTotal = subTotal;
        TotalDiscount = totalDiscount;
        TotalTax = totalTax;
        TotalAdjustment = totalAdjustment;
        GrandTotal = grandTotal;
    }

    public QuotationDetailsModel Update(string? team, Guid? dealId, string? subject, DateTime? validDate, Guid contactId, Guid accountId, string? carrier, string? quoteStage, string? billingStreet, string? billingCity, string? billingState, string? billingCode, string? billingCountry, string? shippingStreet, string? shippingCity, string? shippingState, string? shippingCode, string? shippingCountry, string? description, string? termsConditions, Guid quoteOwnerId, Guid? leadId, List<Quote> quoteItems, string quoteItemsJson, decimal subTotal, decimal totalDiscount, decimal totalTax, decimal totalAdjustment, decimal grandTotal)
    {
        if (team is not null && Team?.Equals(team) is not true) Team = team;
        if (dealId != Guid.Empty && !DealId.Equals(dealId)) DealId = dealId;
        if (subject is not null && Subject?.Equals(subject) is not true) Subject = subject;
        if (ValidDate != validDate) ValidDate = validDate;
        if (contactId != Guid.Empty && !ContactId.Equals(contactId)) ContactId = contactId;
        if (accountId != Guid.Empty && !AccountId.Equals(accountId)) AccountId = accountId;
        if (carrier is not null && Carrier?.Equals(carrier) is not true) Carrier = carrier;
        if (quoteStage is not null && QuoteStage?.Equals(quoteStage) is not true) QuoteStage = quoteStage;
        if (billingStreet is not null && BillingStreet?.Equals(billingStreet) is not true) BillingStreet = billingStreet;
        if (billingCity is not null && BillingCity?.Equals(billingCity) is not true) BillingCity = billingCity;
        if (billingState is not null && BillingState?.Equals(billingState) is not true) BillingState = billingState;
        if (billingCode is not null && BillingCode?.Equals(billingCode) is not true) BillingCode = billingCode;
        if (billingCountry is not null && BillingCountry?.Equals(billingCountry) is not true) BillingCountry = billingCountry;
        if (shippingStreet is not null && ShippingStreet?.Equals(shippingStreet) is not true) ShippingStreet = shippingStreet;
        if (shippingCity is not null && ShippingCity?.Equals(shippingCity) is not true) ShippingCity = shippingCity;
        if (shippingState is not null && ShippingState?.Equals(shippingState) is not true) ShippingState = shippingState;
        if (shippingCode is not null && ShippingCode?.Equals(shippingCode) is not true) ShippingCode = shippingCode;
        if (shippingCountry is not null && BillingCountry?.Equals(billingCountry) is not true) ShippingCountry = shippingCountry;
        if (quoteOwnerId != Guid.Empty && !QuoteOwnerId.Equals(quoteOwnerId)) QuoteOwnerId = quoteOwnerId;
        if (leadId != Guid.Empty && !LeadId.Equals(leadId)) LeadId = leadId;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        if (quoteItems is not null && QuoteItems?.Equals(quoteItems) is not true) QuoteItems = quoteItems;
        if (quoteItemsJson is not null && QuoteItemsJson?.Equals(quoteItemsJson) is not true) QuoteItemsJson = quoteItemsJson;
        if (termsConditions is not null && TermsConditions?.Equals(termsConditions) is not true) TermsConditions = termsConditions;
        if (SubTotal != subTotal) SubTotal = subTotal;
        if (TotalDiscount != totalDiscount) TotalDiscount = totalDiscount;
        if (TotalTax != totalTax) TotalTax = totalTax;
        if (TotalAdjustment != totalAdjustment) TotalAdjustment = totalAdjustment;
        if (GrandTotal != grandTotal) GrandTotal = grandTotal;
        return this;
    }
}

[NotMapped]
public class Quote
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal ListPrice { get; set; }
    public decimal Amount { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalAfterDiscount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Description { get; set; } = string.Empty;

}
