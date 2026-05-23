using FSH.Framework.Core.Domain;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Files.Domain;
using FSH.Modules.Files.Domain.Events;

namespace Integration.Tests.Tests.Files;

/// <summary>
/// Pure-domain coverage for <see cref="FileAsset"/> state transitions and the
/// <see cref="FileSoftDeletedDomainEvent"/> record. These do not touch the web host or storage —
/// they exercise the aggregate's invariants directly to cover the branch edges the HTTP-level
/// Files tests cannot reach (e.g. quarantine-on-infected, idempotent restore, visibility guards).
/// </summary>
public sealed class FileAssetDomainTests
{
    private static FileAsset NewPending(long declaredSize = 1024) => FileAsset.CreatePending(
        id: Guid.CreateVersion7(),
        ownerType: "MyFiles",
        ownerId: null,
        originalFileName: "spec.pdf",
        sanitizedFileName: "spec.pdf",
        contentType: "application/pdf",
        declaredSizeBytes: declaredSize,
        storageKey: "tenants/root/myfiles/2026/05/abc/spec.pdf",
        visibility: Visibility.Private,
        createdByUserId: Guid.NewGuid().ToString(),
        uploadDeadline: DateTimeOffset.UtcNow.AddMinutes(15));

    #region Happy Path

    [Fact]
    public void CreatePending_Should_Start_In_PendingUpload_NotScanned()
    {
        var asset = NewPending();

        asset.Status.ShouldBe(FileAssetStatus.PendingUpload);
        asset.ScanStatus.ShouldBe(ScanStatus.NotScanned);
        asset.UploadDeadline.ShouldNotBeNull();
        asset.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void CreatePending_Should_Generate_Id_When_Guid_Empty()
    {
        var asset = FileAsset.CreatePending(
            id: Guid.Empty,
            ownerType: "MyFiles",
            ownerId: null,
            originalFileName: "x.pdf",
            sanitizedFileName: "x.pdf",
            contentType: "application/pdf",
            declaredSizeBytes: 10,
            storageKey: "k",
            visibility: Visibility.Public,
            createdByUserId: "u1",
            uploadDeadline: DateTimeOffset.UtcNow.AddMinutes(5));

        asset.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void MarkAvailable_Should_Transition_To_Available_And_Raise_FinalizedEvent_When_Clean()
    {
        var asset = NewPending();

        asset.MarkAvailable(actualSize: 2048, scanResult: ScanStatus.Clean);

        asset.Status.ShouldBe(FileAssetStatus.Available);
        asset.SizeBytes.ShouldBe(2048);
        asset.ScanStatus.ShouldBe(ScanStatus.Clean);
        asset.UploadDeadline.ShouldBeNull();
        asset.DomainEvents.OfType<FileFinalizedDomainEvent>().ShouldHaveSingleItem();
    }

    [Fact]
    public void MarkAvailable_Should_Transition_To_Quarantined_When_Infected()
    {
        var asset = NewPending();

        asset.MarkAvailable(actualSize: 2048, scanResult: ScanStatus.Infected);

        asset.Status.ShouldBe(FileAssetStatus.Quarantined);
        asset.ScanStatus.ShouldBe(ScanStatus.Infected);
    }

    [Fact]
    public void ChangeVisibility_Should_Flip_When_Available()
    {
        var asset = NewPending();
        asset.MarkAvailable(2048, ScanStatus.Clean);

        asset.ChangeVisibility(Visibility.Public);

        asset.Visibility.ShouldBe(Visibility.Public);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ChangeVisibility_Should_Be_Idempotent_When_Already_Target()
    {
        var asset = NewPending();
        asset.MarkAvailable(2048, ScanStatus.Clean);
        asset.ChangeVisibility(Visibility.Public);
        var stamp = asset.UpdatedAtUtc;

        asset.ChangeVisibility(Visibility.Public); // no-op — same value

        asset.Visibility.ShouldBe(Visibility.Public);
        asset.UpdatedAtUtc.ShouldBe(stamp); // unchanged: the early return skipped the timestamp bump
    }

    [Fact]
    public void Restore_Should_Be_NoOp_When_Not_Deleted()
    {
        var asset = NewPending();
        asset.MarkAvailable(2048, ScanStatus.Clean);
        var stamp = asset.UpdatedAtUtc;

        asset.Restore(); // not deleted → early return

        asset.IsDeleted.ShouldBeFalse();
        asset.UpdatedAtUtc.ShouldBe(stamp);
    }

    #endregion

    #region Exception

    [Fact]
    public void MarkAvailable_Should_Throw_Conflict_When_Not_Pending()
    {
        var asset = NewPending();
        asset.MarkAvailable(2048, ScanStatus.Clean); // now Available

        var ex = Should.Throw<CustomException>(() => asset.MarkAvailable(4096, ScanStatus.Clean));
        ex.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public void MarkAvailable_Should_Throw_When_ActualSize_NotPositive()
    {
        var asset = NewPending();

        Should.Throw<ArgumentOutOfRangeException>(() => asset.MarkAvailable(0, ScanStatus.Clean));
    }

    [Fact]
    public void ChangeVisibility_Should_Throw_Conflict_When_Still_Pending()
    {
        var asset = NewPending(); // still PendingUpload

        var ex = Should.Throw<CustomException>(() => asset.ChangeVisibility(Visibility.Public));
        ex.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public void ChangeVisibility_Should_Throw_Conflict_When_Quarantined()
    {
        var asset = NewPending();
        asset.MarkAvailable(2048, ScanStatus.Infected); // Quarantined

        var ex = Should.Throw<CustomException>(() => asset.ChangeVisibility(Visibility.Private));
        ex.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public void CreatePending_Should_Throw_When_DeclaredSize_NotPositive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => FileAsset.CreatePending(
            id: Guid.NewGuid(),
            ownerType: "MyFiles",
            ownerId: null,
            originalFileName: "x.pdf",
            sanitizedFileName: "x.pdf",
            contentType: "application/pdf",
            declaredSizeBytes: 0,
            storageKey: "k",
            visibility: Visibility.Private,
            createdByUserId: "u1",
            uploadDeadline: DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreatePending_Should_Throw_When_OwnerType_Blank()
    {
        Should.Throw<ArgumentException>(() => FileAsset.CreatePending(
            id: Guid.NewGuid(),
            ownerType: "   ",
            ownerId: null,
            originalFileName: "x.pdf",
            sanitizedFileName: "x.pdf",
            contentType: "application/pdf",
            declaredSizeBytes: 10,
            storageKey: "k",
            visibility: Visibility.Private,
            createdByUserId: "u1",
            uploadDeadline: DateTimeOffset.UtcNow));
    }

    #endregion

    #region FileSoftDeletedDomainEvent

    [Fact]
    public void FileSoftDeletedDomainEvent_Should_Carry_Identity_And_DomainEvent_Metadata()
    {
        var fileId = Guid.NewGuid();
        var actor = Guid.NewGuid().ToString();

        var evt = DomainEvent.Create((id, ts) =>
            new FileSoftDeletedDomainEvent(fileId, actor, id, ts));

        evt.FileAssetId.ShouldBe(fileId);
        evt.ActorUserId.ShouldBe(actor);
        evt.EventId.ShouldNotBe(Guid.Empty);
        evt.OccurredOnUtc.ShouldBeGreaterThan(DateTimeOffset.MinValue);
        evt.ShouldBeAssignableTo<IDomainEvent>();
    }

    #endregion
}
