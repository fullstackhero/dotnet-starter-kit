using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace Category.Features.Get.v1;
 
public class GetCategoryItemRequest : IRequest<GetCategoryItemResponse>
{
    public Guid Id { get; set; }
    public GetCategoryItemRequest(Guid id) => Id = id;
}
