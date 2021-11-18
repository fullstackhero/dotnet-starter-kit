using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DN.WebApi.Domain.Contracts
{
    public class Filter<T>
    {
        public bool Condition { get; set; }
        public Expression<Func<T, bool>> Expression { get; set; }
    }
}
