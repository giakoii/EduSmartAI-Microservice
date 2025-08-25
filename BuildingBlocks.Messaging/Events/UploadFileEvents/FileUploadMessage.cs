namespace BuildingBlocks.Messaging.Events.UploadFileEvents;

public record FileUploadMessage
{
    public Guid RequestId { get; init; }
    public string FileName { get; init; } = null!;
    public byte[] FileContent { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public long FileSize { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string UserId { get; init; } = null!;
    public Dictionary<string, string>? Metadata { get; init; }
};