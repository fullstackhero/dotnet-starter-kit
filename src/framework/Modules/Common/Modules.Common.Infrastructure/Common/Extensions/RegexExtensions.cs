using System.Text.RegularExpressions;

namespace FSH.Framework.Infrastructure.Common.Extensions;
public static partial class RegexExtensions
{
    private static readonly Regex Whitespace = MyRegex();

    public static string ReplaceWhitespace(this string input, string replacement)
    {
        return Whitespace.Replace(input, replacement);
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MyRegex();
}