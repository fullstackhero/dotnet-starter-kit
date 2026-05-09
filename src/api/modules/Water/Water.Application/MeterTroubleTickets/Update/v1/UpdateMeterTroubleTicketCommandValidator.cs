using FluentValidation;

namespace FSH.Starter.WebApi.Water.Application.MeterTroubleTickets.Update.v1;

public class UpdateMeterTroubleTicketCommandValidator : AbstractValidator<UpdateMeterTroubleTicketCommand>
{
    public UpdateMeterTroubleTicketCommandValidator()
    {
        RuleFor(t => t.IssueDescription).NotEmpty().MaximumLength(1000);
        RuleFor(t => t.ResolutionNotes).MaximumLength(2000);
    }
}
