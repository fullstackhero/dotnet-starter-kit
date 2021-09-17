using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DN.WebApi.Application.Wrapper;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class MapperExtensions
    {
        public static async Task<PaginatedResult<TDto>> ToMappedPaginatedResultAsync<T, TDto>(this IMapper mapper, IQueryable<T> query, int pageNumber, int pageSize)
        where T : class
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            pageNumber = pageNumber == 0 ? 1 : pageNumber;
            pageSize = pageSize == 0 ? 10 : pageSize;
            int count = await query.AsNoTracking().CountAsync();
            pageNumber = pageNumber <= 0 ? 1 : pageNumber;
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            var mappedItems = mapper.Map<List<T>, List<TDto>>(items);
            return PaginatedResult<TDto>.Success(mappedItems, count, pageNumber, pageSize);
        }
    }
}