namespace DN.WebApi.Shared.DTOs;

public interface IMapsterConverter<T, TD>
{
    public TD Convert(T item);

    public T ConvertBack(TD item);
}

public interface IMapsterConverterAsync<T, TD>
{
    public Task<TD> ConvertAsync(T item);

    public Task<T> ConvertBackAsync(TD item);
}