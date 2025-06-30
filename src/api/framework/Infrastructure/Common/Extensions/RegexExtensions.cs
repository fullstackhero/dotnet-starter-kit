using System.Text.RegularExpressions;

namespace FSH.Framework.Infrastructure.Common.Extensions;
public static class RegexExtensions
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.None, TimeSpan.FromMilliseconds(250));

    public static string ReplaceWhitespace(this string input, string replacement)
    {
        return Whitespace.Replace(input, replacement);
    }
}
