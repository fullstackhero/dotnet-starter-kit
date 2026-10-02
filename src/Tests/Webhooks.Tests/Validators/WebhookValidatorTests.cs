using FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Contracts.v1.DeleteWebhookSubscription;
using FSH.Modules.Webhooks.Contracts.v1.TestWebhookSubscription;
using FSH.Modules.Webhooks.Localization;
using Microsoft.Extensions.Localization;
using FSH.Modules.Webhooks.Features.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Features.v1.DeleteWebhookSubscription;
using FSH.Modules.Webhooks.Features.v1.TestWebhookSubscription;
using Webhooks.Tests.Support;

namespace Webhooks.Tests.Validators;

public sealed class WebhookValidatorTests
{
    private static readonly IStringLocalizer<WebhooksResources> Localizer = WebhooksResourcesLocalizerFactory.Create();

    #region CreateWebhookSubscription

    [Fact]
    public void Create_Should_Pass_When_Url_Absolute_And_Events_Present()
    {
        var validator = new CreateWebhookSubscriptionCommandValidator(Localizer);
        var command = new CreateWebhookSubscriptionCommand("https://example.com/hook", ["user.created"], "secret");

        var result = validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Create_Should_Fail_When_Url_Empty()
    {
        var validator = new CreateWebhookSubscriptionCommandValidator(Localizer);
        var command = new CreateWebhookSubscriptionCommand(string.Empty, ["user.created"], null);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateWebhookSubscriptionCommand.Url));
    }

    [Fact]
    public void Create_Should_Fail_When_Url_Not_Absolute()
    {
        var validator = new CreateWebhookSubscriptionCommandValidator(Localizer);
        var command = new CreateWebhookSubscriptionCommand("not-a-url", ["user.created"], null);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "A valid absolute URL is required.");
    }

    [Fact]
    public void Create_Should_Fail_When_Url_Relative()
    {
        var validator = new CreateWebhookSubscriptionCommandValidator(Localizer);
        var command = new CreateWebhookSubscriptionCommand("/relative/path", ["user.created"], null);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_Fail_When_Events_Empty()
    {
        var validator = new CreateWebhookSubscriptionCommandValidator(Localizer);
        var command = new CreateWebhookSubscriptionCommand("https://example.com", [], null);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "At least one event type is required.");
    }

    #endregion

    #region TestWebhookSubscription

    [Fact]
    public void Test_Should_Pass_When_Id_Present()
    {
        var validator = new TestWebhookSubscriptionCommandValidator();
        var command = new TestWebhookSubscriptionCommand(Guid.CreateVersion7());

        validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Test_Should_Fail_When_Id_Empty()
    {
        var validator = new TestWebhookSubscriptionCommandValidator();
        var command = new TestWebhookSubscriptionCommand(Guid.Empty);

        validator.Validate(command).IsValid.ShouldBeFalse();
    }

    #endregion

    #region DeleteWebhookSubscription

    [Fact]
    public void Delete_Should_Pass_When_Id_Present()
    {
        var validator = new DeleteWebhookSubscriptionCommandValidator();
        var command = new DeleteWebhookSubscriptionCommand(Guid.CreateVersion7());

        validator.Validate(command).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Delete_Should_Fail_When_Id_Empty()
    {
        var validator = new DeleteWebhookSubscriptionCommandValidator();
        var command = new DeleteWebhookSubscriptionCommand(Guid.Empty);

        validator.Validate(command).IsValid.ShouldBeFalse();
    }

    #endregion
}
