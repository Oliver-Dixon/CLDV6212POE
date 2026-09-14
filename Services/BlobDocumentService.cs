using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services;

public class BlobDocumentService : IDocumentService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<BlobDocumentService> _log;

    public BlobDocumentService(BlobServiceClient svc, ILogger<BlobDocumentService> log)
    {
        _log = log;
        var name = Environment.GetEnvironmentVariable("DocumentStoreName") ?? "staff-docs";
        _container = svc.GetBlobContainerClient(name);
        _container.CreateIfNotExists();
    }

    public async Task<DocumentMetadata> UploadAsync(
        string fileName, string contentType, Stream content, long length)
    {
        var blob = _container.GetBlobClient(fileName);

        // Streamed straight through — the file is never fully buffered in memory.
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        });

        _log.LogInformation("Uploaded {File} ({Bytes} bytes, {Type})",
            fileName, length, contentType);

        return new DocumentMetadata
        {
            FileName = fileName,
            SizeBytes = length,
            ContentType = contentType,
            LastModified = DateTimeOffset.UtcNow
        };
    }

    public async Task<IReadOnlyList<DocumentMetadata>> ListAsync()
    {
        var results = new List<DocumentMetadata>();

        await foreach (BlobItem item in _container.GetBlobsAsync())
        {
            results.Add(new DocumentMetadata
            {
                FileName = item.Name,
                SizeBytes = item.Properties.ContentLength ?? 0,
                LastModified = item.Properties.LastModified,
                ContentType = item.Properties.ContentType ?? "application/octet-stream"
            });
        }

        _log.LogInformation("Listed {Count} staff documents", results.Count);
        return results;
    }

    public async Task<(Stream, string)?> DownloadAsync(string fileName)
    {
        try
        {
            var blob = _container.GetBlobClient(fileName);
            var download = await blob.DownloadStreamingAsync();

            _log.LogInformation("Downloaded {File}", fileName);

            return (download.Value.Content,
                    download.Value.Details.ContentType ?? "application/octet-stream");
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _log.LogWarning("Download requested for missing file {File}", fileName);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string fileName)
        => await _container.GetBlobClient(fileName).ExistsAsync();
}