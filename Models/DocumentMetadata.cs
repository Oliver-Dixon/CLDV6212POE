namespace CoffeeNChill.Functions.Models;

public class DocumentMetadata
{
    public string FileName { get; set; } = default!;
    public long SizeBytes { get; set; }

    public string SizeDisplay => SizeBytes < 1024
        ? $"{SizeBytes} B"
        : SizeBytes < 1048576
            ? $"{SizeBytes / 1024.0:F1} KB"
            : $"{SizeBytes / 1048576.0:F2} MB";

    public DateTimeOffset? LastModified { get; set; }
    public string ContentType { get; set; } = "application/octet-stream";
}