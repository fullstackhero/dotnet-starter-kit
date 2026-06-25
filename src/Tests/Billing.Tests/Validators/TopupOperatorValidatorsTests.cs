using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Contracts.v1.Wallets;
using FSH.Modules.Billing.Features.v1.Wallets.ApproveTopupRequest;
using FSH.Modules.Billing.Features.v1.Wallets.GetTopupRequests;
using FSH.Modules.Billing.Features.v1.Wallets.RejectTopupRequest;

namespace Billing.Tests.Validators;

public sealed class TopupOperatorValidatorsTests
{
    #region GetTopupRequestsQueryValidator

    [Fact]
    public void GetTopupRequests_Should_Pass_When_Valid()
    {
        var validator = new GetTopupRequestsQueryValidator();

        var result = validator.Validate(new GetTopupRequestsQuery(null, null, 1, 20));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void GetTopupRequests_Should_Pass_With_Optional_Filters()
    {
        var validator = new GetTopupRequestsQueryValidator();

        var result = validator.Validate(new GetTopupRequestsQuery("tenant-1", TopupRequestStatus.Pending, 2, 50));

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 20)]   // PageNumber must be > 0
    [InlineData(-1, 20)]  // PageNumber negative
    [InlineData(1, 0)]    // PageSize must be >= 1
    [InlineData(1, 101)]  // PageSize must be <= 100
    public void GetTopupRequests_Should_Fail_When_Pagination_Invalid(int pageNumber, int pageSize)
    {
        var validator = new GetTopupRequestsQueryValidator();

        var result = validator.Validate(new GetTopupRequestsQuery(null, null, pageNumber, pageSize));

        result.IsValid.ShouldBeFalse();
    }

    #endregion

    #region ApproveTopupRequestCommandValidator

    [Fact]
    public void ApproveTopupRequest_Should_Pass_When_Id_Provided()
    {
        var validator = new ApproveTopupRequestCommandValidator();

        var result = validator.Validate(new ApproveTopupRequestCommand(Guid.NewGuid(), null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ApproveTopupRequest_Should_Pass_When_Note_Provided()
    {
        var validator = new ApproveTopupRequestCommandValidator();

        var result = validator.Validate(new ApproveTopupRequestCommand(Guid.NewGuid(), "approved by ops"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void ApproveTopupRequest_Should_Fail_When_Id_Empty()
    {
        var validator = new ApproveTopupRequestCommandValidator();

        var result = validator.Validate(new ApproveTopupRequestCommand(Guid.Empty, null));

        result.IsValid.ShouldBeFalse();
    }

    #endregion

    #region RejectTopupRequestCommandValidator

    [Fact]
    public void RejectTopupRequest_Should_Pass_When_Valid()
    {
        var validator = new RejectTopupRequestCommandValidator();

        var result = validator.Validate(new RejectTopupRequestCommand(Guid.NewGuid(), "insufficient funds provided"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RejectTopupRequest_Should_Pass_When_Reason_Is_Null()
    {
        var validator = new RejectTopupRequestCommandValidator();

        var result = validator.Validate(new RejectTopupRequestCommand(Guid.NewGuid(), null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RejectTopupRequest_Should_Fail_When_Id_Empty()
    {
        var validator = new RejectTopupRequestCommandValidator();

        var result = validator.Validate(new RejectTopupRequestCommand(Guid.Empty, null));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectTopupRequest_Should_Fail_When_Reason_Exceeds_512_Chars()
    {
        var validator = new RejectTopupRequestCommandValidator();
        var longReason = new string('x', 513);

        var result = validator.Validate(new RejectTopupRequestCommand(Guid.NewGuid(), longReason));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RejectTopupRequest_Should_Pass_When_Reason_Is_Exactly_512_Chars()
    {
        var validator = new RejectTopupRequestCommandValidator();
        var maxReason = new string('x', 512);

        var result = validator.Validate(new RejectTopupRequestCommand(Guid.NewGuid(), maxReason));

        result.IsValid.ShouldBeTrue();
    }

    #endregion
}
