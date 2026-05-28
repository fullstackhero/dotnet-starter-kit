namespace FSH.Modules.Billing.Contracts;

public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    Paid = 2,
    Void = 3
}

public enum SubscriptionStatus
{
    Active = 0,
    Suspended = 1,
    Cancelled = 2
}

public enum InvoiceLineItemKind
{
    BaseFee = 0,
    Overage = 1,
    Adjustment = 2
}

public enum PlanInterval
{
    Monthly = 0,
    Yearly = 1
}

public enum InvoicePurpose
{
    Subscription = 0,
    Usage = 1
}
