using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Persistence;
using FluentValidation;

namespace Category.Features.Create.v1;
 
public class CreateCategoryItemValidator : AbstractValidator<CreateCategoryItemCommand>
{
    public CreateCategoryItemValidator(CategoryItemDbContext context)
    {
        RuleFor(p => p.Name).NotEmpty();
        RuleFor(p => p.Description).NotEmpty();
    }
}
