namespace DN.WebApi.Shared.DTOs;

public interface INotificationMessage
{
    public string MessageType { get; set; }

    public string Message { get; set; }
}