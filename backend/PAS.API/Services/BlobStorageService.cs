using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace PAS.API.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureBlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("AzureBlobStorage:ConnectionString is not configured.");

        var containerName = configuration["AzureBlobStorage:ContainerName"] ?? "submissions";

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Ensure the container exists (creates it if not)
        _containerClient.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> UploadAsync(Stream fileStream, string blobPath, string contentType)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        var headers = new BlobHttpHeaders { ContentType = contentType };

        await blobClient.UploadAsync(fileStream, new BlobUploadOptions
        {
            HttpHeaders = headers
        });

        return blobPath;
    }

    public async Task<Stream> DownloadAsync(string blobPath)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);

        var exists = await blobClient.ExistsAsync();
        if (!exists.Value)
            throw new FileNotFoundException($"Blob '{blobPath}' was not found in storage.");

        var download = await blobClient.DownloadStreamingAsync();
        return download.Value.Content;
    }

    public async Task DeleteAsync(string blobPath)
    {
        var blobClient = _containerClient.GetBlobClient(blobPath);
        await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
    }
}
