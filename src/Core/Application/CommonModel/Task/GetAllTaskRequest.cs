using FL_CRMS_ERP_WEBAPI.Application.CommonModel.Notes;
using FL_CRMS_ERP_WEBAPI.Domain.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPI.Application.CommonModel.Task;
public class GetAllTaskRequest : IRequest<List<TaskModel>>
{
    public GetAllTaskRequest()
    {
            
    }

    public class GetAllTaskRequestHandler : IRequestHandler<GetAllTaskRequest, List<TaskModel>>
    {
        private readonly IRepositoryWithEvents<TaskModel> _repository;

        public GetAllTaskRequestHandler(IRepositoryWithEvents<TaskModel> repository)
        {
            _repository = repository;
        }

        public async Task<List<TaskModel>> Handle(GetAllTaskRequest request, CancellationToken cancellationToken)
        {
            List<TaskModel> task = new List<TaskModel>();
            task = await _repository.ListAsync(cancellationToken);


            return task;
        }
    }
}
