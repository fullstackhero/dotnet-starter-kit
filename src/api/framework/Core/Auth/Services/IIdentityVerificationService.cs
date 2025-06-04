namespace FSH.Framework.Core.Auth.Services;

public interface IIdentityVerificationService
{
    Task<bool> VerifyIdentityAsync(string tckn, string firstName, string lastName, int birthYear);
} 