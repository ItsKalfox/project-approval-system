namespace PAS.API.Services;

public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage and returns the blob path (blob name).
    /// </summary>
    Task<string> UploadAsync(Stream fileStream, string blobPath, string contentType);

    /// <summary>
    /// Downloads a blob by its path and returns the file stream.
    /// </summary>
    Task<Stream> DownloadAsync(string blobPath);

    /// <summary>
    /// Deletes a blob from Azure Blob Storage.
    /// </summary>
    Task DeleteAsync(string blobPath);
}
