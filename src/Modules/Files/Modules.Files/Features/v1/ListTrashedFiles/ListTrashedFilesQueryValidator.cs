using FluentValidation;
using FSH.Modules.Files.Contracts.v1.Queries;

namespace FSH.Modules.Files.Features.v1.ListTrashedFiles;

public sealed class ListTrashedFilesQueryValidator : AbstractValidator<ListTrashedFilesQuery>
{
    public ListTrashedFilesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
