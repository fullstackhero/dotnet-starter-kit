using System.Text.RegularExpressions;

namespace FSH.Modules.Chat.Services;

/// <summary>
/// Pulls <c>@username</c> tokens out of a message body. Conservative pattern matches Slack/Discord:
/// letters / digits / dot / underscore / hyphen. Adjacent punctuation isn't captured.
/// </summary>
public static partial class MentionParser
{
    [GeneratedRegex(@"(?<!\w)@([A-Za-z0-9._-]+)", RegexOptions.CultureInvariant)]
    private static partial Regex MentionRegex();

    public readonly record struct Match(string Username, int StartIndex, int Length);

    public static IReadOnlyList<Match> Parse(string? body)
    {
        if (string.IsNullOrEmpty(body)) return [];
        var results = new List<Match>();
        foreach (System.Text.RegularExpressions.Match match in MentionRegex().Matches(body))
        {
            var username = match.Groups[1].Value;
            if (!string.IsNullOrEmpty(username))
            {
                results.Add(new Match(username, match.Index, match.Length));
            }
        }
        return results;
    }
}
