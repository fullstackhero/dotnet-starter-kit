using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Water.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain;

public class Payment : AuditableEntity, IAggregateRoot
{
    public Guid BillId { get; private set; }
    public virtual Bill Bill { get; private set; } = default!;
    public decimal AmountPaid { get; private set; }
    public DateTimeOffset PaymentDate { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? TransactionReference { get; private set; }
    public PaymentStatus Status { get; private set; }

    private Payment() { }

    private Payment(Guid id, Guid billId, decimal amountPaid, DateTimeOffset paymentDate, PaymentMethod paymentMethod, string? transactionReference)
    {
        Id = id;
        BillId = billId;
        AmountPaid = amountPaid;
        PaymentDate = paymentDate;
        PaymentMethod = paymentMethod;
        TransactionReference = transactionReference;
        Status = PaymentStatus.Completed;

        QueueDomainEvent(new PaymentCreated { Payment = this });
    }

    public static Payment Create(Guid billId, decimal amountPaid, DateTimeOffset paymentDate, PaymentMethod paymentMethod, string? transactionReference)
    {
        return new Payment(Guid.NewGuid(), billId, amountPaid, paymentDate, paymentMethod, transactionReference);
    }
}
