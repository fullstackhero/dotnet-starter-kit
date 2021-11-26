using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DN.WebApi.Application.Abstractions.Services.General
{
    public interface IDatabaseSeeder
    {
        void Initialize();
    }
}