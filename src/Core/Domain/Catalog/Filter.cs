using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog
{

    public class Filter:  AuditableEntity, IAggregateRoot
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public virtual ICollection<GameFilter> GameFilters { get; set; }
    }
}
