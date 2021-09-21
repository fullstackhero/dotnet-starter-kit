using System;
using System.Linq;
using DN.WebApi.Shared.DTOs;

/*using AutoMapper;*/

namespace DN.WebApi.Infrastructure.Persistence.Converters
{
    /*public class OrderByConverter :
        IValueConverter<string, string[]>,
        IValueConverter<string[], string>
    {
        public string[] Convert(string orderBy, ResolutionContext context = null)
        {
            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                return orderBy
                    .Split(',')
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim()).ToArray();
            }

            return Array.Empty<string>();
        }

        public string Convert(string[] orderBy, ResolutionContext context = null) => orderBy?.Any() == true ? string.Join(",", orderBy) : null;
    }*/
    public class OrderByConverter : IMapsterConverter<string, string[]>
    {
        public string[] Convert(string item)
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

        public string ConvertBack(string[] item)
        {
            return item?.Any() == true ? string.Join(",", item) : null;
        }
    }
}