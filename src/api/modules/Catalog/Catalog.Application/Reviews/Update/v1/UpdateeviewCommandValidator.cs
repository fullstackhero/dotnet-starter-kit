using FluentValidation;

namespace FSH.Starter.WebApi.Catalog.Application.Reviews.Update.v1;
public class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(b => b.Reviewer).NotEmpty().MinimumLength(2).MaximumLength(100);
        RuleFor(b => b.Content).MaximumLength(1000);
        
    }
}
