using System.Text.RegularExpressions;

namespace DN.WebApi.Infrastructure.Extensions
{
    public static class RegexExtensions
    {
        private static readonly Regex Whitespace = new Regex(@"\s+");
        public static string ReplaceWhitespace(string input, string replacement)
        {
            return Whitespace.Replace(input, replacement);
        }
    }
}