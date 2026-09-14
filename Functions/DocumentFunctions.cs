using Azure;
using CoffeeNChill.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class DocumentFunctions
{
    private readonly IDocumentService _docs;
    private readonly ILogger<DocumentFunctions> _log;

    private const long MaxBytes = 25 * 1024 * 1024;   // 25 MB

    private static readonly string[] AllowedTypes =
    {
        "application/pdf",
        "image/png",
        "image/jpeg",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public DocumentFunctions(IDocumentService docs, ILogger<DocumentFunctions> log)
    {
        _docs = docs;
        _log = log;
    }

    // POST /api/documents/upload
    [Function("UploadStaffDocument")]
    public async Task<IActionResult> Upload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")]
        HttpRequest req)
    {
        try
        {
            if (!req.HasFormContentType)
                return new BadRequestObjectResult(new
                {
                    error = "Content-Type must be multipart/form-data."
                });

            var form = await req.ReadFormAsync();

            if (form.Files.Count == 0)
                return new BadRequestObjectResult(new { error = "No file was supplied." });

            var file = form.Files[0];

            if (file.Length == 0)
                return new BadRequestObjectResult(new { error = "The uploaded file is empty." });

            if (file.Length > MaxBytes)
                return new BadRequestObjectResult(new
                {
                    error = "File exceeds the 25 MB limit.",
                    sizeBytes = file.Length
                });

            if (!AllowedTypes.Contains(file.ContentType))
                return new BadRequestObjectResult(new
                {
                    error = $"Unsupported file type '{file.ContentType}'.",
                    allowed = AllowedTypes
                });

            // Strip any directory component — blocks ../../etc path traversal
            var safeName = Path.GetFileName(file.FileName);

            if (string.IsNullOrWhiteSpace(safeName) || safeName.Contains(".."))
                return new BadRequestObjectResult(new { error = "Invalid file name." });

            if (await _docs.ExistsAsync(safeName))
                return new ConflictObjectResult(new
                {
                    error = $"A document named '{safeName}' already exists."
                });

            await using var stream = file.OpenReadStream();
            var metadata = await _docs.UploadAsync(
                safeName, file.ContentType, stream, file.Length);

            return new CreatedResult($"/api/documents/download/{safeName}", metadata);
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "Storage failure during document upload");
            return new ObjectResult(new { error = "Storage service unavailable." })
            { StatusCode = 503 };
        }
    }

    // GET /api/documents
    [Function("ListStaffDocuments")]
    public async Task<IActionResult> List(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")]
        HttpRequest req)
    {
        try
        {
            var documents = await _docs.ListAsync();
            return new OkObjectResult(documents);
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "Storage failure while listing documents");
            return new ObjectResult(new { error = "Storage service unavailable." })
            { StatusCode = 503 };
        }
    }

    // GET /api/documents/download/{fileName}
    [Function("DownloadStaffDocument")]
    public async Task<IActionResult> Download(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")]
        HttpRequest req,
        string fileName)
    {
        try
        {
            var safeName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeName))
                return new BadRequestObjectResult(new { error = "File name is required." });

            var result = await _docs.DownloadAsync(safeName);

            if (result is null)
                return new NotFoundObjectResult(new
                {
                    error = $"Document '{safeName}' was not found in staff-docs."
                });

            // FileStreamResult streams to the client without buffering
            return new FileStreamResult(result.Value.Content, result.Value.ContentType)
            {
                FileDownloadName = safeName
            };
        }
        catch (RequestFailedException ex)
        {
            _log.LogError(ex, "Storage failure while downloading {File}", fileName);
            return new ObjectResult(new { error = "Storage service unavailable." })
            { StatusCode = 503 };
        }
    }
}