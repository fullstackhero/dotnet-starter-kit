using System.Runtime.Serialization;

namespace DN.WebApi.Shared.DTOs.Multitenancy;

[DataContract]
public class UpgradeSubscriptionRequest
{
    [DataMember(Order = 1)]
    public string Tenant { get; set; }

    [DataMember(Order = 2)]
    public DateTime ExtendedExpiryDate { get; set; }
}