using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog
{
    public class GameFilter : AuditableEntitySimple
    {
        public DefaultIdType GameId { get; set; }
        public virtual Game Game { get; set; }
        public DefaultIdType FilterId { get; set; }
        public virtual Filter Filter {get;set;}
    }
}
