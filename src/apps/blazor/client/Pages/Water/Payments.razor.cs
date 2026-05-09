using FSH.Starter.Blazor.Client.Components.EntityTable;
using FSH.Starter.Blazor.Infrastructure.Api;
using FSH.Starter.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Components;

namespace FSH.Starter.Blazor.Client.Pages.Water;

public partial class Payments
{
    [Inject]
    protected IApiClient _client { get; set; } = default!;

    protected EntityServerTableContext<PaymentResponse, Guid, PaymentViewModel> Context { get; set; } = default!;

    private EntityTable<PaymentResponse, Guid, PaymentViewModel> _table = default!;

    protected override void OnInitialized() =>
        Context = new(
            entityName: "Payment",
            entityNamePlural: "Payments",
            entityResource: FshResources.Payments,
            fields: new()
            {
                new(payment => payment.Id, "Id", "Id"),
                new(payment => payment.BillId, "Bill Id", "BillId"),
                new(payment => payment.AmountPaid, "Amount Paid", "AmountPaid"),
                new(payment => payment.PaymentDate, "Payment Date", "PaymentDate"),
                new(payment => payment.PaymentMethod, "Payment Method", "PaymentMethod"),
                new(payment => payment.TransactionReference, "Transaction Reference", "TransactionReference"),
                new(payment => payment.Status, "Status", "Status")
            },
            enableAdvancedSearch: true,
            idFunc: payment => payment.Id!.Value,
            searchFunc: async filter =>
            {
                var paymentFilter = filter.Adapt<SearchPaymentsCommand>();
                var result = await _client.SearchPaymentsEndpointAsync("1", paymentFilter);
                return result.Adapt<PaginationResponse<PaymentResponse>>();
            },
            createFunc: async payment =>
            {
                await _client.CreatePaymentEndpointAsync("1", payment.Adapt<CreatePaymentCommand>());
            });
}

public class PaymentViewModel : CreatePaymentCommand
{
}
