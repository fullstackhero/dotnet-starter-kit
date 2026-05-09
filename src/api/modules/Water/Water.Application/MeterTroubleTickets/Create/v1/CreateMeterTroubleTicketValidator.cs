using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Create.v1;

public sealed class CreateMeterTroubleTicketCommandValidator : AbstractValidator<CreateMeterTroubleTicketCommand>
{
    public CreateMeterTroubleTicketCommandValidator()
    {
        RuleFor(t => t.IssueDescription).NotEmpty().MaximumLength(1000);
    }
}
