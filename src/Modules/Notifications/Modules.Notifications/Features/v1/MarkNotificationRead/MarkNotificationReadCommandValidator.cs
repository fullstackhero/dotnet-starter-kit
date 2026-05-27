using FluentValidation;
using FSH.Modules.Notifications.Contracts.v1.Commands;

namespace FSH.Modules.Notifications.Features.v1.MarkNotificationRead;

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
