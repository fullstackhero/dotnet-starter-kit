using System;

namespace DN.WebApi.Domain.Contracts
{
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; }

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
        }
    }
}