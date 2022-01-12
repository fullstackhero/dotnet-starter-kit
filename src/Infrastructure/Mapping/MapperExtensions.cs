using FSH.WebApi.Application.Common.Models;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace FSH.WebApi.Infrastructure.Mapping;

public static class MapperExtensions
{
    public static Task<PaginationResponse<TDto>> ToMappedPaginatedResultAsync<T, TDto>(
        this IQueryable<T> query, int pageNumber, int pageSize, in CancellationToken cancellationToken = default)
    where T : class =>
        new MappedPaginatedResultConverter<T, TDto>(pageNumber, pageSize)
            .ConvertBackAsync(query, cancellationToken);

    private class MappedPaginatedResultConverter<T, TDto> : IMapsterConverterAsync<PaginationResponse<TDto>, IQueryable<T>>
    where T : class
    {
        private int _pageNumber;
        private int _pageSize;

        public MappedPaginatedResultConverter(int pageNumber, int pageSize) =>
            (_pageNumber, _pageSize) = (pageNumber, pageSize);

        public Task<IQueryable<T>> ConvertAsync(PaginationResponse<TDto> item, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public async Task<PaginationResponse<TDto>> ConvertBackAsync(IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            // throw exception if query is null
            ArgumentNullException.ThrowIfNull(query);

            _pageNumber = _pageNumber == 0 ? 1 : _pageNumber;
            _pageSize = _pageSize == 0 ? 10 : _pageSize;
            int count = await query.AsNoTracking().CountAsync(cancellationToken: cancellationToken);
            _pageNumber = _pageNumber <= 0 ? 1 : _pageNumber;
            var items = await query.Skip((_pageNumber - 1) * _pageSize).Take(_pageSize).ToListAsync(cancellationToken);
            var mappedItems = items.Adapt<List<TDto>>();
            return new PaginationResponse<TDto>(mappedItems, count, _pageNumber, _pageSize);
        }
    }
}