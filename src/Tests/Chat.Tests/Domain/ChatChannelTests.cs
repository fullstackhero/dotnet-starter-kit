using FSH.Modules.Chat.Domain;
using FSH.Modules.Chat.Domain.Events;

namespace Chat.Tests.Domain;

public class ChatChannelTests
{
    #region Happy Path

    [Fact]
    public void CreateChannel_Should_Add_Creator_As_Admin_With_Slug_And_Domain_Event()
    {
        var c = ChatChannel.CreateChannel("Engineering Team", "All things eng", isPrivate: false, "user-1");

        c.Type.ShouldBe(ChannelType.Channel);
        c.Name.ShouldBe("Engineering Team");
        c.Slug.ShouldBe("engineering-team");
        c.IsPrivate.ShouldBeFalse();
        c.Members.ShouldHaveSingleItem();
        c.Members[0].UserId.ShouldBe("user-1");
        c.Members[0].Role.ShouldBe(ChannelMemberRole.Admin);
        c.DomainEvents.ShouldContain(e => e is ChannelCreatedDomainEvent);
    }

    [Fact]
    public void CreateDirect_Should_Sort_DirectKey_And_Add_Both_Members()
    {
        var a = ChatChannel.CreateDirect("zeta-user", "alpha-user");
        var b = ChatChannel.CreateDirect("alpha-user", "zeta-user");

        a.DirectKey.ShouldBe("alpha-user:zeta-user");
        b.DirectKey.ShouldBe(a.DirectKey);
        a.Type.ShouldBe(ChannelType.DirectMessage);
        a.IsPrivate.ShouldBeTrue();
        a.Members.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateGroupDm_Should_Mark_Creator_As_Admin_And_Others_As_Members()
    {
        var g = ChatChannel.CreateGroupDm(["u1", "u2", "u3"], "u2");

        g.Type.ShouldBe(ChannelType.GroupMessage);
        g.IsPrivate.ShouldBeTrue();
        g.Members.Count.ShouldBe(3);
        g.Members.Single(m => m.UserId == "u2").Role.ShouldBe(ChannelMemberRole.Admin);
        g.Members.Single(m => m.UserId == "u1").Role.ShouldBe(ChannelMemberRole.Member);
    }

    [Fact]
    public void AddMember_Should_Append_Member_And_Raise_Event()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");

        c.AddMember("u2", "u1");

        c.Members.Count.ShouldBe(2);
        c.DomainEvents.ShouldContain(e => e is ChannelMemberAddedDomainEvent);
    }

    [Fact]
    public void RemoveMember_Should_Drop_Member_And_Raise_Event()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");
        c.AddMember("u2", "u1");

        c.RemoveMember("u2", "u1");

        c.Members.Count.ShouldBe(1);
        c.DomainEvents.ShouldContain(e => e is ChannelMemberRemovedDomainEvent);
    }

    [Fact]
    public void MarkRead_Should_Advance_Watermark_For_Member()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");
        var msgId = Guid.CreateVersion7();

        c.MarkRead("u1", msgId);

        c.Members.Single(m => m.UserId == "u1").LastReadMessageId.ShouldBe(msgId);
    }

    [Fact]
    public void Restore_Should_Be_Idempotent_When_Not_Deleted()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");
        c.Restore();
        c.IsDeleted.ShouldBeFalse();
    }

    #endregion

    #region Exceptions

    [Fact]
    public void CreateDirect_Should_Reject_Self_DM()
    {
        Should.Throw<ArgumentException>(() => ChatChannel.CreateDirect("u1", "u1"));
    }

    [Fact]
    public void CreateGroupDm_Should_Reject_Fewer_Than_Three_Members()
    {
        Should.Throw<ArgumentException>(() => ChatChannel.CreateGroupDm(["u1", "u2"], "u1"));
    }

    [Fact]
    public void AddMember_Should_Reject_DM_Channels()
    {
        var dm = ChatChannel.CreateDirect("u1", "u2");

        Should.Throw<InvalidOperationException>(() => dm.AddMember("u3", "u1"));
    }

    [Fact]
    public void AddMember_Should_Reject_Duplicate()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");

        Should.Throw<InvalidOperationException>(() => c.AddMember("u1", "u1"));
    }

    [Fact]
    public void RemoveMember_Should_Reject_Unknown_User()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");

        Should.Throw<InvalidOperationException>(() => c.RemoveMember("u-ghost", "u1"));
    }

    [Fact]
    public void Rename_Should_Reject_Non_Channel_Types()
    {
        var dm = ChatChannel.CreateDirect("u1", "u2");

        Should.Throw<InvalidOperationException>(() => dm.Rename("nope", null));
    }

    [Fact]
    public void SetPrivate_Should_Reject_Non_Channel_Types()
    {
        var dm = ChatChannel.CreateDirect("u1", "u2");

        Should.Throw<InvalidOperationException>(() => dm.SetPrivate(false));
    }

    [Fact]
    public void MarkRead_Should_Reject_Non_Member()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");

        Should.Throw<InvalidOperationException>(() => c.MarkRead("u-ghost", Guid.CreateVersion7()));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Slugify_Should_Collapse_NonAlphanumerics_And_Repeats()
    {
        var c = ChatChannel.CreateChannel("  Hello  -- World!! ", null, false, "u1");
        c.Slug.ShouldBe("hello-world");
    }

    [Fact]
    public void TouchLastMessage_Should_Update_Both_Timestamps()
    {
        var c = ChatChannel.CreateChannel("eng", null, false, "u1");
        var t = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        c.TouchLastMessage(t);

        c.LastMessageAtUtc.ShouldBe(t);
        c.UpdatedAtUtc.ShouldBe(t);
    }

    #endregion
}
