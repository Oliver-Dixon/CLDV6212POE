using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Staff operational documents. All routes require Admin or Staff.</summary>
[ApiController]
[Route("api/documents")]
[Authorize(Roles = "Admin,Staff")]
public class DocumentsController : ControllerBase
{
    private readonly IHttpClientFactory _factory;

    public DocumentsController(IHttpClientFactory factory) => _factory = factory;

    /// <summary>Lists stored documents with size and last modified date.</summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List()
    {
        var client = _factory.CreateClient("functions");
        using var response = await client.GetAsync("api/documents");
        var payload = await response.Content.ReadAsStringAsync();

        return new ContentResult
        {
            Content = payload,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    /// <summary>Uploads a document. PDF, PNG, JPEG and Word only.</summary>
    /// <response code="201">Uploaded.</response>
    /// <response code="400">No file, empty file, or blocked file type.</response>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file was supplied." });

        var client = _factory.CreateClient("functions");

        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        using var response = await client.PostAsync("api/documents/upload", content);
        var payload = await response.Content.ReadAsStringAsync();

        return new ContentResult
        {
            Content = payload,
            ContentType = "application/json",
            StatusCode = (int)response.StatusCode
        };
    }

    /// <summary>Streams a stored document back to the caller.</summary>
    /// <response code="200">The file.</response>
    /// <response code="404">No such document.</response>
    [HttpGet("download/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string fileName)
    {
        var client = _factory.CreateClient("functions");
        var response = await client.GetAsync(
            $"api/documents/download/{Uri.EscapeDataString(fileName)}");

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return new ContentResult
            {
                Content = error,
                ContentType = "application/json",
                StatusCode = (int)response.StatusCode
            };
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var contentType = response.Content.Headers.ContentType?.MediaType
                          ?? "application/octet-stream";

        return File(stream, contentType, fileName);
    }
}