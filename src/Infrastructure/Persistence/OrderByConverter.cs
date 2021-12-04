using DN.WebApi.Shared.DTOs;

namespace DN.WebApi.Infrastructure.Persistence;

public class OrderByConverter : IMapsterConverter<string?, string[]>
{
    public string[] Convert(string? item)
    {
        if (!string.IsNullOrWhiteSpace(item))
        {
            return item
                .Split(',')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()).ToArray();
        }

        return Array.Empty<string>();
    }

    public string? ConvertBack(string[]? item)
    {
        return item?.Any() == true ? string.Join(",", item) : null;
    }
}