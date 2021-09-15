using System;

namespace DN.WebApi.Domain.Contracts
{
    public interface ISoftDelete
    {
        DateTime? DeletedOn { get; set; }
        Guid? DeletedBy { get; set; }
    }
}