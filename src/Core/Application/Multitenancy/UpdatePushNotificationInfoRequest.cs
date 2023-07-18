using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Application.Multitenancy;
public sealed record UpdatePushNotificationInfoRequest(string Id, TenantPushNotificationInfo PushNotificationInfo)
    : IRequest<string>;

public class UpdatePushNotificationInfoRequestValidator : CustomValidator<UpdatePushNotificationInfoRequest>
{
    public UpdatePushNotificationInfoRequestValidator() =>
        RuleFor(t => t.Id)
            .NotEmpty();
}

public class UpdatePushNotificationInfoRequestHandler : IRequestHandler<UpdatePushNotificationInfoRequest, string>
{
    private readonly ITenantService _tenantService;

    public UpdatePushNotificationInfoRequestHandler(ITenantService tenantService) => _tenantService = tenantService;

    public Task<string> Handle(UpdatePushNotificationInfoRequest request, CancellationToken cancellationToken) =>
        _tenantService.UpdatePushNotificationInfo(request.Id, request.PushNotificationInfo);
}