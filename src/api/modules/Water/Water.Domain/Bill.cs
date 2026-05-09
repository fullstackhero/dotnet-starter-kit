using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Domain.Contracts;
using FSH.Starter.WebApi.Water.Domain.Events;

namespace FSH.Starter.WebApi.Water.Domain;

public class Bill : AuditableEntity, IAggregateRoot
{
    public Guid CustomerId { get; private set; }
    public virtual Customer Customer { get; private set; } = default!;
    public Guid? TariffId { get; private set; }
    public virtual Tariff? Tariff { get; private set; }
    public int BillingMonth { get; private set; }
    public int BillingYear { get; private set; }
    public decimal TotalConsumption { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal FixedCharge { get; private set; }
    public decimal VariableCharge { get; private set; }
    public DateTimeOffset DueDate { get; private set; }
    public DateTimeOffset? PaidDate { get; private set; }
    public BillStatus Status { get; private set; }

    private Bill() { }

    private Bill(Guid id, Guid customerId, Guid? tariffId, int billingMonth, int billingYear, decimal totalConsumption, decimal fixedCharge, decimal variableCharge, decimal totalAmount, DateTimeOffset dueDate)
    {
        Id = id;
        CustomerId = customerId;
        TariffId = tariffId;
        BillingMonth = billingMonth;
        BillingYear = billingYear;
        TotalConsumption = totalConsumption;
        FixedCharge = fixedCharge;
        VariableCharge = variableCharge;
        TotalAmount = totalAmount;
        DueDate = dueDate;
        Status = BillStatus.Draft;

        QueueDomainEvent(new BillCreated { Bill = this });
    }

    public static Bill Create(Guid customerId, Guid? tariffId, int billingMonth, int billingYear, decimal totalConsumption, decimal fixedCharge, decimal variableCharge, decimal totalAmount, DateTimeOffset dueDate)
    {
        return new Bill(Guid.NewGuid(), customerId, tariffId, billingMonth, billingYear, totalConsumption, fixedCharge, variableCharge, totalAmount, dueDate);
    }

    public Bill MarkAsPublished()
    {
        if (Status == BillStatus.Draft)
        {
            Status = BillStatus.Published;
        }

        return this;
    }

    public Bill MarkAsPaid(DateTimeOffset paidDate)
    {
        if (Status == BillStatus.Published || Status == BillStatus.Overdue)
        {
            Status = BillStatus.Paid;
            PaidDate = paidDate;
            QueueDomainEvent(new BillPaid { Bill = this });
        }

        return this;
    }

    public Bill MarkAsOverdue()
    {
        if (Status == BillStatus.Published)
        {
            Status = BillStatus.Overdue;
        }

        return this;
    }

    public Bill Cancel()
    {
        if (Status != BillStatus.Paid)
        {
            Status = BillStatus.Cancelled;
            QueueDomainEvent(new BillCancelled { Bill = this });
        }

        return this;
    }
}
