using FSH.Framework.Core.Common.Events;

namespace FSH.Framework.Core.Auth.Events;

public class UserRegisteredEvent : DomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string Tckn { get; }
    public string FirstName { get; }
    public string LastName { get; }

    public UserRegisteredEvent(
        Guid userId,
        string email,
        string tckn,
        string firstName,
        string lastName)
    {
        UserId = userId;
        Email = email;
        Tckn = tckn;
        FirstName = firstName;
        LastName = lastName;
    }
} 