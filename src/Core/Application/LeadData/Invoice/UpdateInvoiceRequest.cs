using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Invoice;
public class UpdateInvoiceRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public Guid InvoiceOwnerId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid AccountId { get; set; }
    public decimal ExciseDuty { get; set; }
    public Guid? InvoiceStatusId { get; set; }
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
    public string[]? ContactIds { get; set; }
    //public int IsActive { get; set; }
    public string? Reason { get; set; }
    public int InvoiceId { get; set; }
    public class UpdateInvoiceRequestHandler : IRequestHandler<UpdateInvoiceRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<InvoiceDetailsModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateInvoiceRequestHandler(IRepositoryWithEvents<InvoiceDetailsModel> repository, IStringLocalizer<UpdateInvoiceRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateInvoiceRequest request, CancellationToken cancellationToken)
        {
            var invoice = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = invoice
            ?? throw new NotFoundException(_t["Invoice {0} Not Found.", request.Id]);

            invoice.Update(request.InvoiceOwnerId, request.ContactId, request.AccountId, request.ExciseDuty, request.InvoiceStatusId, request.Subject, request.InvoiceDate, request.DueDate,
                request.SalesCommission, request.BillingStreet, request.BillingCity, request.BillingState, request.BillingCode, request.BillingCountry, request.ShippingStreet, request.ShippingCity, request.ShippingState,
                request.ShippingCode, request.ShippingCountry, request.InvoiceItems, request.InvoiceItemsJson, request.SubTotal, request.TotalDiscount, request.TotalTax, request.TotalAdjustment, request.GrandTotal, request.QuoteId, request.CustomerInsuranceId, request.LeadId,
                request.TermsConditions, request.Description, request.ContactIds, request.Reason, request.InvoiceId);

            await _repository.UpdateAsync(invoice, cancellationToken);

            return request.Id;
        }
    }
}
