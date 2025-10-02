using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Category.Persistence;
using FluentValidation;

namespace Category.Features.Update.v1;
 
public class UpdateCategoryItemValidator : AbstractValidator<UpdateCategoryItemCommand>
{
    public UpdateCategoryItemValidator(CategoryItemDbContext context)
    {
        RuleFor(p => p.Name).NotEmpty();
        RuleFor(p => p.Description).NotEmpty();
    }
}
