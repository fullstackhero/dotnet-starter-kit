using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FSH.Framework.Core.Auth.Services;

namespace FSH.Framework.Core.Auth.Features.Identity;

public class TestMernisRequestHandler : IRequestHandler<TestMernisRequest, TestMernisResult>
{
    private readonly IIdentityVerificationService _identityService;
    private readonly ILogger<TestMernisRequestHandler> _logger;

    public TestMernisRequestHandler(
        IIdentityVerificationService identityService,
        ILogger<TestMernisRequestHandler> logger)
    {
        _identityService = identityService;
        _logger = logger;
    }

    public async Task<TestMernisResult> Handle(TestMernisRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing MERNİS test for TC Kimlik: {TcKimlik}", request.Tckn);

        var isValid = await _identityService.VerifyIdentityAsync(
            request.Tckn,
            request.FirstName,
            request.LastName,
            request.BirthYear);

        _logger.LogInformation("MERNİS test completed for TC Kimlik: {TcKimlik}, Result: {IsValid}", 
            request.Tckn, isValid);

        return new TestMernisResult
        {
            IsValid = isValid,
            Message = isValid ? "Identity verification successful" : "Identity verification failed"
        };
    }
}