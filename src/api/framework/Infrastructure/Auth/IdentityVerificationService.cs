using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Infrastructure.Auth;

public class IdentityVerificationService : IIdentityVerificationService
{
    public async Task<bool> VerifyIdentityAsync(string tckn, string firstName, string lastName, int birthYear)
    {
        // TODO: Implement actual MERNİS (Turkish National Identity Service) integration
        // This is a stub implementation for development purposes
        
        // For now, we'll simulate the verification process with a delay
        await Task.Delay(100);
        
        // In a real implementation, this would:
        // 1. Connect to MERNİS web service
        // 2. Send the TCKN, first name, last name, and birth year
        // 3. Return the verification result from the government service
        
        // For development/testing, we'll accept all valid TCKN formats
        return IsValidTCKN(tckn);
    }
    
    private static bool IsValidTCKN(string tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn) || tckn.Length != 11)
        {
            return false;
        }

        if (!tckn.All(char.IsDigit))
        {
            return false;
        }

        // TCKN cannot start with 0
        if (tckn[0] == '0')
        {
            return false;
        }

        // Algorithm for Turkish National ID validation
        var digits = tckn.Select(c => int.Parse(c.ToString())).ToArray();

        var sumOdd = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var sumEven = digits[1] + digits[3] + digits[5] + digits[7];

        var check1 = ((sumOdd * 7) - sumEven) % 10;
        var check2 = (sumOdd + sumEven + check1) % 10;

        return check1 == digits[9] && check2 == digits[10];
    }
} 