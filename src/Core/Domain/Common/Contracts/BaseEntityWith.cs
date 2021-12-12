using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Domain.Common.Contracts;

public abstract class BaseEntityWith<T> : BaseEntity
{
    public T Id { get; protected set; }
}

public abstract class BaseEntityWith : BaseEntityWith<Guid>
{

    protected BaseEntityWith()
    {
        Id = NewId.Next().ToGuid();
    }
}