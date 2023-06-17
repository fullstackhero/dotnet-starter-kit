using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Domain.LeadData;
public class InvoiceDetailsModel : AuditableEntity, IAggregateRoot
{
    
    public Guid InvoiceOwnerId { get; set; }
    public Guid ContactId { get; set; }
    public Guid AccountId { get; set; }
    public decimal ExciseDuty { get; set; }
    public int InvoiceStatusId { get; set; }
    public string? Subject { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal SalesCommission { get; set; }
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

    [NotMapped]
    public List<Quote> InvoiceItems { get; set; }

    [Column(TypeName = "jsonb")]
    public string InvoiceItemsJson
    {
        get
        {
            return JsonConvert.SerializeObject(InvoiceItems);
        }
        set
        {
            InvoiceItems = JsonConvert.DeserializeObject<List<Quote>>(value);
        }
    }

    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAdjustment { get; set; }
    public decimal GrandTotal { get; set; }
    public Guid QuoteId { get; set; }
    public Guid CustomerInsuranceId { get; set; }

    //public int? IsDeleted { get; set; }
    //public string CompanyId { get; set; }
    public Guid LeadId { get; set; }
    public string? TermsConditions { get; set; }
    public string? Description { get; set; }

    public InvoiceDetailsModel(Guid invoiceOwnerId, Guid contactId, Guid accountId, decimal exciseDuty, int invoiceStatusId, string? subject, DateTime? invoiceDate, DateTime? dueDate, decimal salesCommission, string? billingStreet, string? billingCity, string? billingState, string? billingCode, string? billingCountry, string? shippingStreet, string? shippingCity, string? shippingState, string? shippingCode, string? shippingCountry, string invoiceItemsJson, decimal subTotal, decimal totalDiscount, decimal totalTax, decimal totalAdjustment, decimal grandTotal, Guid quoteId, Guid customerInsuranceId, Guid leadId, string? termsConditions, string? description)
    {
        InvoiceOwnerId = invoiceOwnerId;
        ContactId = contactId;
        AccountId = accountId;
        ExciseDuty = exciseDuty;
        InvoiceStatusId = invoiceStatusId;
        Subject = subject;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        SalesCommission = salesCommission;
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
        InvoiceItems = new List<Quote>();
        InvoiceItemsJson = invoiceItemsJson;
        SubTotal = subTotal;
        TotalDiscount = totalDiscount;
        TotalTax = totalTax;
        TotalAdjustment = totalAdjustment;
        GrandTotal = grandTotal;
        QuoteId = quoteId;
        CustomerInsuranceId = customerInsuranceId;
        LeadId = leadId;
        TermsConditions = termsConditions;
        Description = description;
    }

    public InvoiceDetailsModel Update(Guid invoiceOwnerId, Guid contactId, Guid accountId, decimal exciseDuty, int invoiceStatusId, string? subject, DateTime? invoiceDate, DateTime? dueDate, decimal salesCommission, string? billingStreet, string? billingCity, string? billingState, string? billingCode, string? billingCountry, string? shippingStreet, string? shippingCity, string? shippingState, string? shippingCode, string? shippingCountry, List<Quote> invoiceItems, string invoiceItemsJson, decimal subTotal, decimal totalDiscount, decimal totalTax, decimal totalAdjustment, decimal grandTotal, Guid quoteId, Guid customerInsuranceId, Guid leadId, string? termsConditions, string? description)
    {
        if (invoiceOwnerId != Guid.Empty && !InvoiceOwnerId.Equals(invoiceOwnerId)) InvoiceOwnerId = invoiceOwnerId;
        if (contactId != Guid.Empty && !ContactId.Equals(contactId)) ContactId = contactId;
        if (accountId != Guid.Empty && !AccountId.Equals(accountId)) AccountId = accountId;
        if (ExciseDuty != exciseDuty) ExciseDuty = exciseDuty;
        if (InvoiceStatusId != invoiceStatusId) InvoiceStatusId = invoiceStatusId;
        if (subject is not null && Subject?.Equals(subject) is not true) Subject = subject;
        if (InvoiceDate != invoiceDate) InvoiceDate = invoiceDate;
        if (DueDate != dueDate) DueDate = dueDate;
        if (SalesCommission != salesCommission) SalesCommission = salesCommission;
        if (billingStreet is not null && BillingStreet?.Equals(billingStreet) is not true) BillingStreet = billingStreet;
        if (shippingStreet is not null && ShippingStreet?.Equals(shippingStreet) is not true) ShippingStreet = shippingStreet;
        if (billingCity is not null && BillingCity?.Equals(billingCity) is not true) BillingCity = billingCity;
        if (shippingCity is not null && ShippingCity?.Equals(shippingCity) is not true) ShippingCity = shippingCity;
        if (billingCode is not null && BillingCode?.Equals(billingCode) is not true) BillingCode = billingCode;
        if (shippingCode is not null && ShippingCode?.Equals(shippingCode) is not true) ShippingCode = shippingCode;
        if (billingCountry is not null && BillingCountry?.Equals(billingCountry) is not true) BillingCountry = billingCountry;
        if (shippingCountry is not null && ShippingCountry?.Equals(shippingCountry) is not true) ShippingCountry = shippingCountry;
        if (shippingState is not null && ShippingState?.Equals(shippingState) is not true) ShippingState = shippingState;
        if (billingState is not null && BillingState?.Equals(billingState) is not true) BillingState = billingState;
        if (invoiceItemsJson is not null && InvoiceItemsJson?.Equals(invoiceItemsJson) is not true) InvoiceItemsJson = invoiceItemsJson;
        if (SubTotal != subTotal) SubTotal = subTotal;
        if (TotalDiscount != totalDiscount) TotalDiscount = totalDiscount;
        if (TotalTax != totalTax) TotalTax = totalTax;
        if (TotalAdjustment != totalAdjustment) TotalAdjustment = totalAdjustment;
        if (GrandTotal != grandTotal) GrandTotal = grandTotal;
        if (quoteId != Guid.Empty && !QuoteId.Equals(quoteId)) QuoteId = quoteId;
        if (customerInsuranceId != Guid.Empty && !CustomerInsuranceId.Equals(customerInsuranceId)) CustomerInsuranceId = customerInsuranceId;
        if (leadId != Guid.Empty && !LeadId.Equals(leadId)) LeadId = leadId;
        if (termsConditions is not null && TermsConditions?.Equals(termsConditions) is not true) TermsConditions = termsConditions;
        if (description is not null && Description?.Equals(description) is not true) Description = description;
        return this;
    }
}
