using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services;

public interface IDocumentService
{
    Task<DocumentMetadata> UploadAsync(string fileName, string contentType, Stream content, long length);
    Task<IReadOnlyList<DocumentMetadata>> ListAsync();
    Task<(Stream Content, string ContentType)?> DownloadAsync(string fileName);
    Task<bool> ExistsAsync(string fileName);
}