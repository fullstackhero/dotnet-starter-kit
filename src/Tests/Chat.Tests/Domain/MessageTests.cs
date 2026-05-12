using FSH.Modules.Chat.Domain;
using FSH.Modules.Chat.Domain.Events;

namespace Chat.Tests.Domain;

public class MessageTests
{
    #region Happy Path

    [Fact]
    public void Create_Should_Trim_Body_And_Raise_Created_Event()
    {
        var channelId = Guid.CreateVersion7();

        var m = Message.Create(channelId, "u1", "  hi there  ");

        m.ChannelId.ShouldBe(channelId);
        m.AuthorUserId.ShouldBe("u1");
        m.Body.ShouldBe("hi there");
        m.ParentMessageId.ShouldBeNull();
        m.DeletedAtUtc.ShouldBeNull();
        m.DomainEvents.ShouldContain(e => e is MessageCreatedDomainEvent);
    }

    [Fact]
    public void Edit_Should_Update_Body_And_Stamp_EditedAt_When_Author()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "old");

        m.Edit("new body", "u1");

        m.Body.ShouldBe("new body");
        m.EditedAtUtc.ShouldNotBeNull();
        m.DomainEvents.ShouldContain(e => e is MessageEditedDomainEvent);
    }

    [Fact]
    public void SoftDelete_Should_Tombstone_Body_And_Stamp_DeletedAt_When_Author()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "secret");

        m.SoftDelete("u1", isModerator: false);

        m.DeletedAtUtc.ShouldNotBeNull();
        m.Body.ShouldBeNull();
        m.DomainEvents.ShouldContain(e => e is MessageDeletedDomainEvent);
    }

    [Fact]
    public void SoftDelete_Should_Allow_Moderator_Even_When_Not_Author()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "x");

        m.SoftDelete("admin-user", isModerator: true);

        m.DeletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void IncrementReplyCount_Should_Bump_Counter()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "x");

        m.IncrementReplyCount();
        m.IncrementReplyCount();

        m.ReplyCount.ShouldBe(2);
    }

    [Fact]
    public void DecrementReplyCount_Should_Floor_At_Zero()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "x");

        m.DecrementReplyCount();
        m.DecrementReplyCount();

        m.ReplyCount.ShouldBe(0);
    }

    [Fact]
    public void AddAttachment_Should_Append_To_Attachments_List()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "see file");

        var att = m.AddAttachment(Guid.NewGuid(), "https://x", "image/png", "f.png", 100);

        m.Attachments.ShouldHaveSingleItem();
        m.Attachments[0].ShouldBe(att);
    }

    #endregion

    #region Exceptions

    [Fact]
    public void Create_Should_Reject_Empty_Channel_Id()
    {
        Should.Throw<ArgumentException>(() => Message.Create(Guid.Empty, "u1", "hi"));
    }

    [Fact]
    public void Create_Should_Reject_Empty_Body()
    {
        Should.Throw<ArgumentException>(() => Message.Create(Guid.CreateVersion7(), "u1", "   "));
    }

    [Fact]
    public void Edit_Should_Reject_Non_Author()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "x");

        Should.Throw<InvalidOperationException>(() => m.Edit("hijack", "u2"));
    }

    [Fact]
    public void Edit_Should_Reject_When_Already_Deleted()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "x");
        m.SoftDelete("u1", isModerator: false);

        Should.Throw<InvalidOperationException>(() => m.Edit("y", "u1"));
    }

    [Fact]
    public void SoftDelete_Should_Reject_Non_Author_When_Not_Moderator()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "x");

        Should.Throw<InvalidOperationException>(() => m.SoftDelete("u2", isModerator: false));
    }

    [Fact]
    public void SoftDelete_Should_Be_Idempotent()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "x");
        m.SoftDelete("u1", isModerator: false);
        var firstDeletedAt = m.DeletedAtUtc;

        m.SoftDelete("u1", isModerator: false);

        m.DeletedAtUtc.ShouldBe(firstDeletedAt);
    }

    #endregion

    #region Reactions

    [Fact]
    public void AddReaction_Should_Append_Row_For_New_User_Emoji_Pair()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "hi");

        var added = m.AddReaction("u2", "🚀");

        added.ShouldNotBeNull();
        m.Reactions.ShouldHaveSingleItem();
        m.Reactions[0].UserId.ShouldBe("u2");
        m.Reactions[0].Emoji.ShouldBe("🚀");
    }

    [Fact]
    public void AddReaction_Should_Return_Null_When_Duplicate()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "hi");
        m.AddReaction("u2", "🚀");

        var duplicate = m.AddReaction("u2", "🚀");

        duplicate.ShouldBeNull();
        m.Reactions.Count.ShouldBe(1);
    }

    [Fact]
    public void AddReaction_Should_Reject_When_Message_Deleted()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "hi");
        m.SoftDelete("u1", isModerator: false);

        Should.Throw<InvalidOperationException>(() => m.AddReaction("u2", "🚀"));
    }

    [Fact]
    public void RemoveReaction_Should_Remove_When_Present()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "hi");
        m.AddReaction("u2", "🚀");

        var removed = m.RemoveReaction("u2", "🚀");

        removed.ShouldBeTrue();
        m.Reactions.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveReaction_Should_Return_False_When_Absent()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "hi");

        m.RemoveReaction("u2", "🚀").ShouldBeFalse();
    }

    [Fact]
    public void AddReaction_Should_Trim_Emoji_For_Comparison()
    {
        var m = Message.Create(Guid.CreateVersion7(), "u1", "hi");
        m.AddReaction("u2", " 🚀 ");

        var dup = m.AddReaction("u2", "🚀");

        dup.ShouldBeNull("emoji is trimmed before equality comparison");
    }

    #endregion
}
