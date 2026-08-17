using System.Net;

namespace FSH.Framework.Mailing;

/// <summary>
/// The single HTML shell and the single encoder for outbound mail. Bodies are sent as
/// <c>text/html</c>, so any value reaching the markup has to be encoded or it is parsed as markup
/// rather than shown. Keeping both here means a module cannot ship its own weaker escaping.
/// </summary>
/// <remarks>
/// Pair every HTML body with a <c>text/plain</c> alternative on <see cref="MailRequest.TextBody"/>:
/// HTML-only mail leaves text-only clients with nothing and scores worse with spam filters.
/// </remarks>
public static class HtmlEmail
{
    /// <summary>
    /// HTML-encodes a value for insertion into markup. Covers quotes and apostrophes as well as
    /// <c>&amp;</c>, <c>&lt;</c> and <c>&gt;</c>, so the same call is safe in an attribute and in
    /// element content.
    /// </summary>
    public static string Encode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return WebUtility.HtmlEncode(value);
    }

    /// <summary>
    /// Wraps already-built markup in the shared document: doctype, charset, viewport, and the
    /// centred card every e-mail from the kit renders in.
    /// </summary>
    /// <param name="heading">Plain text. Encoded here, and also used as the document title.</param>
    /// <param name="innerHtml">
    /// TRUSTED markup, inserted verbatim and NOT encoded. Build it from literals plus
    /// <see cref="Encode(string)"/>d values; never pass user input straight through.
    /// </param>
    public static string Shell(string heading, string innerHtml)
    {
        ArgumentNullException.ThrowIfNull(heading);
        ArgumentNullException.ThrowIfNull(innerHtml);

        string safeHeading = Encode(heading);

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
                                        {innerHtml}
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
    /// A message whose point is a single action link, rendered as a real anchor so mail clients make
    /// it clickable. The address is repeated as text underneath for clients that strip buttons.
    /// </summary>
    public static string LinkAction(string heading, string intro, string actionUrl, string actionLabel)
    {
        ArgumentNullException.ThrowIfNull(intro);
        ArgumentNullException.ThrowIfNull(actionUrl);
        ArgumentNullException.ThrowIfNull(actionLabel);

        string safeIntro = Encode(intro);
        string safeUrl = Encode(actionUrl);
        string safeLabel = Encode(actionLabel);

        return Shell(heading, $"""
            <p style="margin: 0 0 24px 0; font-size: 15px; line-height: 1.6; color: #334155;">{safeIntro}</p>
                                        <p style="margin: 0 0 24px 0;">
                                            <a href="{safeUrl}" style="display: inline-block; padding: 12px 24px; background-color: #0f172a; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 15px;">{safeLabel}</a>
                                        </p>
                                        <p style="margin: 0; font-size: 13px; line-height: 1.6; color: #64748b;">
                                            If the button does not work, copy this address into your browser:<br>
                                            <a href="{safeUrl}" style="color: #2563eb; word-break: break-all;">{safeUrl}</a>
                                        </p>
            """);
    }

    /// <summary>
    /// A short informational message with no action link.
    /// </summary>
    public static string Notice(string heading, string message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return Shell(heading, $"""<p style="margin: 0; font-size: 15px; line-height: 1.6; color: #334155;">{Encode(message)}</p>""");
    }
}
