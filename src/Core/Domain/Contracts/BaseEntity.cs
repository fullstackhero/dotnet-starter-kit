using System;
using System.Collections.Generic;
using MassTransit;

namespace DN.WebApi.Domain.Contracts
{
    public abstract class BaseEntity
    {
        public Guid Id { get; private set; }
        public List<DomainEvent> DomainEvents = new();

        protected BaseEntity()
        {
            Id = NewId.Next().ToGuid();
        }
    }
}