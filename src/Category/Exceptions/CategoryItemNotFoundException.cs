using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSH.Framework.Core.Exceptions;

namespace Category.Exceptions;
 
internal sealed class CategoryItemNotFoundException : NotFoundException
{
    public CategoryItemNotFoundException(Guid id)
        : base($"category item with id {id} not found")
    {
    }
}
