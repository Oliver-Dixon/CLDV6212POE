using Azure.Data.Tables;
using Azure.Storage.Blobs;
using CoffeeNChill.Functions.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        var conn = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                   ?? "UseDevelopmentStorage=true";

        // Storage clients registered once and reused — creating them per
        // request would waste connections and slow every call down.
        services.AddSingleton(new TableServiceClient(conn));
        services.AddSingleton(new BlobServiceClient(conn));

        services.AddSingleton<IMenuService, MenuService>();
        services.AddSingleton<IDocumentService, BlobDocumentService>();
        services.AddSingleton<IQueueService, QueueService>();
        services.AddSingleton<IOrderService, OrderService>();
    })
    .Build();

builder.Run();