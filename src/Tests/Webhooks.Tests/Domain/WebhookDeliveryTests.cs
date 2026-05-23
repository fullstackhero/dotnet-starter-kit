using FSH.Modules.Webhooks.Domain;

namespace Webhooks.Tests.Domain;

public sealed class WebhookDeliveryTests
{
    #region Happy Path

    [Fact]
    public void Create_Should_Default_To_First_Attempt_When_Attempt_Not_Specified()
    {
        var subId = Guid.CreateVersion7();

        var delivery = WebhookDelivery.Create(subId, "user.created", "{}");

        delivery.SubscriptionId.ShouldBe(subId);
        delivery.EventType.ShouldBe("user.created");
        delivery.PayloadJson.ShouldBe("{}");
        delivery.AttemptCount.ShouldBe(1);
        delivery.Id.ShouldNotBe(Guid.Empty);
        delivery.AttemptedAtUtc.ShouldNotBe(default);
        delivery.Success.ShouldBeFalse();
        delivery.HttpStatusCode.ShouldBe(0);
        delivery.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Create_Should_Track_Retry_Attempt_Number()
    {
        var delivery = WebhookDelivery.Create(Guid.CreateVersion7(), "user.created", "{}", attemptNumber: 3);

        delivery.AttemptCount.ShouldBe(3);
    }

    [Fact]
    public void RecordResult_Should_Mark_Success_With_Status_And_No_Error()
    {
        var delivery = WebhookDelivery.Create(Guid.CreateVersion7(), "user.created", "{}");

        delivery.RecordResult(200, success: true, errorMessage: null);

        delivery.HttpStatusCode.ShouldBe(200);
        delivery.Success.ShouldBeTrue();
        delivery.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void RecordResult_Should_Capture_Failure_Status_And_Error_Message()
    {
        var delivery = WebhookDelivery.Create(Guid.CreateVersion7(), "user.created", "{}");

        delivery.RecordResult(500, success: false, errorMessage: "Internal Server Error");

        delivery.HttpStatusCode.ShouldBe(500);
        delivery.Success.ShouldBeFalse();
        delivery.ErrorMessage.ShouldBe("Internal Server Error");
    }

    [Fact]
    public void RecordResult_Should_Overwrite_Previous_Result_On_Subsequent_Call()
    {
        var delivery = WebhookDelivery.Create(Guid.CreateVersion7(), "user.created", "{}");
        delivery.RecordResult(500, success: false, errorMessage: "first failure");

        delivery.RecordResult(200, success: true, errorMessage: null);

        delivery.Success.ShouldBeTrue();
        delivery.HttpStatusCode.ShouldBe(200);
        delivery.ErrorMessage.ShouldBeNull();
    }

    #endregion

    #region Exceptions

    [Fact]
    public void Create_Should_Throw_When_Attempt_Number_Is_Zero()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            WebhookDelivery.Create(Guid.CreateVersion7(), "user.created", "{}", attemptNumber: 0));
    }

    [Fact]
    public void Create_Should_Throw_When_Attempt_Number_Is_Negative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            WebhookDelivery.Create(Guid.CreateVersion7(), "user.created", "{}", attemptNumber: -5));
    }

    #endregion
}
