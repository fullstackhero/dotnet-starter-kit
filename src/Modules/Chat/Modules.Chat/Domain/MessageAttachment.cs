using FSH.Framework.Core.Domain;

namespace FSH.Modules.Chat.Domain;

public sealed class MessageAttachment : BaseEntity<Guid>
{
    public Guid MessageId { get; private set; }
    public Guid? FileAssetId { get; private set; }
    public string Url { get; private set; } = default!;
    public string ContentType { get; private set; } = default!;
    public string OriginalFileName { get; private set; } = default!;
    public long SizeBytes { get; private set; }

    private MessageAttachment() { }

    internal static MessageAttachment Create(
        Guid messageId,
        Guid? fileAssetId,
        string url,
        string contentType,
        string fileName,
        long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return new MessageAttachment
        {
            Id = Guid.CreateVersion7(),
            MessageId = messageId,
            FileAssetId = fileAssetId,
            Url = url,
            ContentType = contentType,
            OriginalFileName = fileName,
            SizeBytes = sizeBytes,
        };
    }
}
