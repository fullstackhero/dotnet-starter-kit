using DN.WebApi.Application.Wrapper;
using DN.WebApi.Shared.DTOs;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace DN.WebApi.Infrastructure.Mapping;

public static class MapperExtensions
{
    public static Task<PaginatedResult<TDto>> ToMappedPaginatedResultAsync<T, TDto>(
        this IQueryable<T> query, int pageNumber, int pageSize, in CancellationToken cancellationToken = default)
    where T : class =>
        new MappedPaginatedResultConverter<T, TDto>(pageNumber, pageSize)
            .ConvertBackAsync(query, cancellationToken);

    private class MappedPaginatedResultConverter<T, TDto> : IMapsterConverterAsync<PaginatedResult<TDto>, IQueryable<T>>
    where T : class
    {
        private int _pageNumber;
        private int _pageSize;

        public MappedPaginatedResultConverter(int pageNumber, int pageSize) =>
            (_pageNumber, _pageSize) = (pageNumber, pageSize);

        public Task<IQueryable<T>> ConvertAsync(PaginatedResult<TDto> item, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public async Task<PaginatedResult<TDto>> ConvertBackAsync(IQueryable<T> query, CancellationToken cancellationToken = default)
        {
            // throw exception if query is null
            ArgumentNullException.ThrowIfNull(query);

            _pageNumber = _pageNumber == 0 ? 1 : _pageNumber;
            _pageSize = _pageSize == 0 ? 10 : _pageSize;
            int count = await query.AsNoTracking().CountAsync();
            _pageNumber = _pageNumber <= 0 ? 1 : _pageNumber;
            var items = await query.Skip((_pageNumber - 1) * _pageSize).Take(_pageSize).ToListAsync(cancellationToken);
            var mappedItems = items.Adapt<List<TDto>>();
            return await Task.FromResult(PaginatedResult<TDto>.Success(mappedItems, count, _pageNumber, _pageSize));
        }
    }
}