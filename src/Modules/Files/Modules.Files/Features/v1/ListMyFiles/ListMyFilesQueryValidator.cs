using FluentValidation;
using FSH.Modules.Files.Contracts.v1.Queries;

namespace FSH.Modules.Files.Features.v1.ListMyFiles;

public sealed class ListMyFilesQueryValidator : AbstractValidator<ListMyFilesQuery>
{
    public ListMyFilesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
