using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Messaging.Events.UploadFileEvents;

public static class FormFileExtensions
{
    public static async Task<FileUploadMessage> ToFileUploadMessageAsync(this IFormFile formFile, Guid requestId, string userId)
    {
        byte[] fileContent;
        await using (var memoryStream = new MemoryStream())
        {
            await formFile.CopyToAsync(memoryStream);
            fileContent = memoryStream.ToArray();
        }

        return new FileUploadMessage
        {
            RequestId = requestId,
            FileName = formFile.FileName,
            FileContent = fileContent,
            ContentType = formFile.ContentType,
            FileSize = formFile.Length,
            UserId = userId
        };
    }
}

public record UploadStatusEntity
{
    public required Guid RequestId { get; init; }
    public required string UserId { get; init; }
    public required string Status { get; init; }
    public string? FileUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
}