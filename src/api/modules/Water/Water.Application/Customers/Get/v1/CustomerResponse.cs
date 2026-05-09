using FSH.Starter.WebApi.Water.Domain;

namespace FSH.Starter.WebApi.Water.Application.Customers.Get.v1;

public sealed record CustomerResponse(
    Guid? Id,
    string CustomerCode,
    string FullName,
    string? Address,
    string? ContactNumber,
    string? Email,
    ConnectionType ConnectionType,
    CustomerStatus Status);
