using FSH.Framework.Core.Exceptions;
using FSH.Modules.Files.Contracts.v1.DTOs;
using FSH.Modules.Files.Domain;

namespace Files.Tests.Domain;

public class FileAssetTests
{
    private static FileAsset NewPending(Visibility visibility = Visibility.Public) =>
        FileAsset.CreatePending(
            id: Guid.NewGuid(),
            ownerType: "Product",
            ownerId: Guid.NewGuid(),
            originalFileName: "x.png",
            sanitizedFileName: "x.png",
            contentType: "image/png",
            declaredSizeBytes: 1024,
            storageKey: "tenants/t/product/2026/05/abc/x.png",
            visibility: visibility,
            createdByUserId: "user-1",
            uploadDeadline: DateTimeOffset.UtcNow.AddMinutes(15));

    [Fact]
    public void CreatePending_Should_StartInPendingUpload()
    {
        var f = NewPending();
        f.Status.ShouldBe(FileAssetStatus.PendingUpload);
        f.ScanStatus.ShouldBe(ScanStatus.NotScanned);
        f.UploadDeadline.ShouldNotBeNull();
        f.SizeBytes.ShouldBe(1024);
    }

    [Fact]
    public void CreatePending_Should_GenerateIdWhenEmpty()
    {
        var f = FileAsset.CreatePending(
            id: Guid.Empty,
            ownerType: "MyFiles",
            ownerId: null,
            originalFileName: "x.pdf",
            sanitizedFileName: "x.pdf",
            contentType: "application/pdf",
            declaredSizeBytes: 100,
            storageKey: "k",
            visibility: Visibility.Private,
            createdByUserId: "u",
            uploadDeadline: DateTimeOffset.UtcNow.AddMinutes(1));
        f.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void CreatePending_Should_RejectNegativeSize()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            FileAsset.CreatePending(Guid.NewGuid(), "Product", null, "x.png", "x.png",
                "image/png", -1, "k", Visibility.Public, "u", DateTimeOffset.UtcNow.AddMinutes(1)));
    }

    [Fact]
    public void MarkAvailable_Should_TransitionToAvailable_When_ScanClean()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Clean);
        f.Status.ShouldBe(FileAssetStatus.Available);
        f.SizeBytes.ShouldBe(2048);
        f.ScanStatus.ShouldBe(ScanStatus.Clean);
        f.UploadDeadline.ShouldBeNull();
        f.UpdatedAtUtc.ShouldNotBeNull();
        f.DomainEvents.ShouldHaveSingleItem();
    }

    [Fact]
    public void MarkAvailable_Should_TransitionToQuarantined_When_ScanInfected()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Infected);
        f.Status.ShouldBe(FileAssetStatus.Quarantined);
        f.ScanStatus.ShouldBe(ScanStatus.Infected);
    }

    [Fact]
    public void MarkAvailable_Should_Reject_When_NotPendingUpload()
    {
        var f = NewPending();
        f.MarkAvailable(2048, ScanStatus.Clean);

        var ex = Should.Throw<CustomException>(() => f.MarkAvailable(4096, ScanStatus.Clean));
        ex.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
    }

    [Fact]
    public void Restore_Should_BeIdempotent_When_NotDeleted()
    {
        var f = NewPending();
        f.Restore();
        f.IsDeleted.ShouldBeFalse();
    }
}
