using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Api.Controllers;

/// <summary>Shared forwarding logic for controllers that proxy to the Functions app.</summary>
public abstract class GatewayControllerBase : ControllerBase
{
    private readonly IHttpClientFactory _factory;

    protected GatewayControllerBase(IHttpClientFactory factory) => _factory = factory;

    /// <summary>Forwards a request to the Functions app and relays its response verbatim.</summary>
    protected async Task<IActionResult> ForwardAsync(
        HttpMethod method, string path, object? body = null)
    {
        var client = _factory.CreateClient("functions");
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        var payload = await response.Content.ReadAsStringAsync();

        // The Functions app's status code is preserved, so a 404 stays a 404.
        return new ContentResult
        {
            Content = payload,
            ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/json",
            StatusCode = (int)response.StatusCode
        };
    }
}