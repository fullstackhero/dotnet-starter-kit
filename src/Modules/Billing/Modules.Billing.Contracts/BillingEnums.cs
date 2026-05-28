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
    // Usage is 0 (the CLR default) so it doubles as the column default: existing rows backfill to
    // Usage, and a Subscription invoice (1) is always written explicitly. Do NOT reorder — making
    // Subscription 0 reintroduces the EF "CLR-default value is omitted, DB default wins" bug.
    Usage = 0,
    Subscription = 1
}
