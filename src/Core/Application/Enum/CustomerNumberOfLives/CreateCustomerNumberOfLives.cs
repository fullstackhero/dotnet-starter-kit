using FL_CRMS_ERP_WEBAPI.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FL_CRMS_ERP_WEBAPIFL_CRMS_ERP_WEBAPIApplication.Enum.CustomerNumberOfLives;

public class CreateCustomerNumberOfLives : IRequest<DefaultIdType>
{
    public string? TotalLives { get; set; }
}

public class CreateCustomerNumberOfLivesHandler : IRequestHandler<CreateCustomerNumberOfLives, DefaultIdType>
{
    private readonly IRepositoryWithEvents<CustomerNumberOfLivesModel> _repository;

    public CreateCustomerNumberOfLivesHandler(IRepositoryWithEvents<CustomerNumberOfLivesModel> repository) => _repository = repository;

    public async Task<DefaultIdType> Handle(CreateCustomerNumberOfLives request, CancellationToken cancellationToken)
    {
        var returnValue = new CustomerNumberOfLivesModel(request.TotalLives);

        await _repository.AddAsync(returnValue, cancellationToken);

        return returnValue.Id;
    }
}
