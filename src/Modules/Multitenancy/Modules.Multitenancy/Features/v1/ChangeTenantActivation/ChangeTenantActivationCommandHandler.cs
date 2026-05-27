using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.ChangeTenantActivation;
using Mediator;

namespace FSH.Modules.Multitenancy.Features.v1.ChangeTenantActivation;

public sealed class ChangeTenantActivationCommandHandler : ICommandHandler<ChangeTenantActivationCommand, TenantLifecycleResultDto>
{
    private readonly ITenantService _tenantService;

    public ChangeTenantActivationCommandHandler(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public async ValueTask<TenantLifecycleResultDto> Handle(ChangeTenantActivationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string message;

        if (command.IsActive)
        {
            message = await _tenantService.ActivateAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            message = await _tenantService.DeactivateAsync(command.TenantId, cancellationToken).ConfigureAwait(false);
        }

        var status = await _tenantService.GetStatusAsync(command.TenantId, cancellationToken).ConfigureAwait(false);

        return new TenantLifecycleResultDto
        {
            TenantId = status.Id,
            IsActive = status.IsActive,
            ValidUpto = status.ValidUpto,
            Message = message
        };
    }
}