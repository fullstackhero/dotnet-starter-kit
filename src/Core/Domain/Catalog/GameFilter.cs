using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog
{

    public class GameFilter:  AuditableEntity, IAggregateRoot
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }
}
