using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.WebApi.Domain.Catalog
{
    public class Game : AuditableEntity, IAggregateRoot
    {
        public string Name { get; set; }
        public int MinPlayer { get; set; }
        public int MaxPlayer { get; set; }
        public string ManagerId { get; set; }
        public string Address { get; set; }
        public double Price { get; set; }
        public DefaultIdType GameTypeId { get; set; }
        public virtual GameType GameType { get; set; }
        public DateTime DateTime {get;set;}
        public virtual ICollection<GameFilter> GameFilters { get; set; }


    }
}
