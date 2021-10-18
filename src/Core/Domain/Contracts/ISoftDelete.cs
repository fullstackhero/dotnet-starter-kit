using System;

namespace DN.WebApi.Domain.Contracts
{
    public interface ISoftDelete
    {
        public bool IsDeleted { get; set; }
    }
}