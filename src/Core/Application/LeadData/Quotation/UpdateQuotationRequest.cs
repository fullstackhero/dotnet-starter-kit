using FL_CRMS_ERP_WEBAPI.Application.LeadData.Account;
using FL_CRMS_ERP_WEBAPI.Domain.LeadData;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.LeadData.Quotation;
public class UpdateQuotationRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string? Team { get; set; }
    public Guid DealId { get; set; }
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

    public Guid LeadId { get; set; }

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

    public class UpdateQuotationRequestHandler : IRequestHandler<UpdateQuotationRequest, Guid>
    {
        // Add Domain Events automatically by using IRepositoryWithEvents
        private readonly IRepositoryWithEvents<QuotationDetailsModel> _repository;
        private readonly IStringLocalizer _t;

        public UpdateQuotationRequestHandler(IRepositoryWithEvents<QuotationDetailsModel> repository, IStringLocalizer<UpdateQuotationRequestHandler> localizer) =>
            (_repository, _t) = (repository, localizer);

        public async Task<Guid> Handle(UpdateQuotationRequest request, CancellationToken cancellationToken)
        {
            var quotation = await _repository.GetByIdAsync(request.Id, cancellationToken);

            _ = quotation
            ?? throw new NotFoundException(_t["Quotation {0} Not Found.", request.Id]);

            quotation.Update(request.Team, request.DealId, request.Subject, request.ValidDate, request.ContactId, request.AccountId, request.Carrier, request.QuoteStage, request.BillingStreet,
                request.ShippingStreet, request.BillingCity, request.ShippingCity, request.BillingCode, request.ShippingCode, request.BillingCountry, request.ShippingCountry, request.BillingState, request.ShippingState,
                request.Description, request.TermsConditions, request.QuoteOwnerId, request.LeadId, request.QuoteItems, request.QuoteItemsJson, request.SubTotal, request.TotalDiscount, request.TotalTax, request.TotalAdjustment, request.GrandTotal);

            await _repository.UpdateAsync(quotation, cancellationToken);

            return request.Id;
        }
    }
}
