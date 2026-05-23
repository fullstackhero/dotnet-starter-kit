using System.Collections.ObjectModel;
using FSH.Framework.Mailing;

namespace Framework.Tests.Mailing;

public sealed class MailRequestTests
{
    #region Happy Path

    [Fact]
    public void Ctor_Should_AssignProvidedValues_When_FullArgsGiven()
    {
        // Arrange
        var to = new Collection<string> { "a@x.com" };
        var cc = new Collection<string> { "c@x.com" };
        var bcc = new Collection<string> { "b@x.com" };
        var attachments = new Dictionary<string, byte[]> { ["file.txt"] = [1, 2] };
        var headers = new Dictionary<string, string> { ["X-Test"] = "1" };

        // Act
        var request = new MailRequest(
            to, "subject", "body", "from@x.com", "Sender",
            "reply@x.com", "Reply", bcc, cc, attachments, headers);

        // Assert
        request.To.ShouldBe(to);
        request.Subject.ShouldBe("subject");
        request.Body.ShouldBe("body");
        request.From.ShouldBe("from@x.com");
        request.DisplayName.ShouldBe("Sender");
        request.ReplyTo.ShouldBe("reply@x.com");
        request.ReplyToName.ShouldBe("Reply");
        request.Cc.ShouldBe(cc);
        request.Bcc.ShouldBe(bcc);
        request.AttachmentData.ShouldContainKey("file.txt");
        request.Headers["X-Test"].ShouldBe("1");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Ctor_Should_DefaultCollections_When_OptionalArgsOmitted()
    {
        // Arrange & Act
        var request = new MailRequest(new Collection<string> { "a@x.com" }, "subject");

        // Assert — nullable collections default to empty (never null).
        request.Body.ShouldBeNull();
        request.From.ShouldBeNull();
        request.Cc.ShouldNotBeNull();
        request.Cc.ShouldBeEmpty();
        request.Bcc.ShouldNotBeNull();
        request.Bcc.ShouldBeEmpty();
        request.AttachmentData.ShouldNotBeNull();
        request.AttachmentData.ShouldBeEmpty();
        request.Headers.ShouldNotBeNull();
        request.Headers.ShouldBeEmpty();
    }

    #endregion
}
