using System.Net;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Builds the HTML bodies for identity e-mails. Every value that reaches the markup is HTML-encoded:
/// bodies are sent as <c>text/html</c>, so an unescaped name or URL is parsed as markup rather than shown.
/// </summary>
internal static class EmailBodies
{
    /// <summary>
    /// A message whose point is a single action link, rendered as a real anchor so mail clients make it
    /// clickable. Callers pair this with a text/plain alternative carrying the same URL.
    /// </summary>
    internal static string LinkActionHtml(string heading, string intro, string actionUrl, string actionLabel)
    {
        string safeHeading = WebUtility.HtmlEncode(heading);
        string safeIntro = WebUtility.HtmlEncode(intro);
        string safeUrl = WebUtility.HtmlEncode(actionUrl);
        string safeLabel = WebUtility.HtmlEncode(actionLabel);

        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{safeHeading}</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;">
                <table role="presentation" style="width: 100%; border-collapse: collapse;">
                    <tr>
                        <td align="center" style="padding: 40px 0;">
                            <table role="presentation" style="width: 100%; max-width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                                <tr>
                                    <td style="padding: 40px;">
                                        <h1 style="margin: 0 0 16px 0; font-size: 22px; color: #0f172a;">{safeHeading}</h1>
                                        <p style="margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: #334155;">{safeIntro}</p>
                                        <p style="margin: 0 0 24px 0;">
                                            <a href="{safeUrl}" style="display: inline-block; padding: 12px 24px; background-color: #0f172a; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 15px;">{safeLabel}</a>
                                        </p>
                                        <p style="margin: 0; font-size: 13px; line-height: 1.6; color: #64748b;">
                                            If the button does not work, copy this address into your browser:<br>
                                            <a href="{safeUrl}" style="color: #2563eb; word-break: break-all;">{safeUrl}</a>
                                        </p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// A short informational message with no action link.
    /// </summary>
    internal static string NoticeHtml(string heading, string message)
    {
        string safeHeading = WebUtility.HtmlEncode(heading);
        string safeMessage = WebUtility.HtmlEncode(message);

        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>{safeHeading}</title>
            </head>
            <body style="margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; background-color: #f8fafc;">
                <table role="presentation" style="width: 100%; border-collapse: collapse;">
                    <tr>
                        <td align="center" style="padding: 40px 0;">
                            <table role="presentation" style="width: 100%; max-width: 600px; border-collapse: collapse; background-color: #ffffff; border-radius: 8px; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);">
                                <tr>
                                    <td style="padding: 40px;">
                                        <h1 style="margin: 0 0 16px 0; font-size: 22px; color: #0f172a;">{safeHeading}</h1>
                                        <p style="margin: 0; font-size: 15px; line-height: 1.6; color: #334155;">{safeMessage}</p>
                                    </td>
                                </tr>
                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
            """;
    }
}
