namespace DN.WebApi.Shared.DTOs.General.Requests;

public class MailRequest
{
    public MailRequest(string to, string subject, string? body = null, string? from = null) =>
        (To, Subject, Body, From) = (to, subject, body, from);

    public string To { get; }

    public string Subject { get; }

    public string? Body { get; }

    public string? From { get; }
}