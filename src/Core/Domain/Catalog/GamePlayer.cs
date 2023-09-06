using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog
{
    public class GamePlayer : AuditableEntitySimple
    {
        public DefaultIdType GameId { get; set; }
        public virtual Game Game{get;set;}

        public DefaultIdType PlayerId { get; set; }
        public virtual Player Player { get; set; }
    }
}
