namespace DN.WebApi.Shared.DTOs.General.Requests;

public class MailRequest
{
    public string To { get; set; }

    public string Subject { get; set; }

    public string Body { get; set; }

    public string From { get; set; }
}