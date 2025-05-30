using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task Write(Guid applicationId, Guid blobId, Stream stream, bool isPublicAccess = false)
    {
        var containerClient = await GetContainerClient(applicationId, isPublicAccess);
        var blobClient = containerClient.GetBlobClient(blobId.ToString());
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task<Stream> Read(Guid applicationId, Guid blobId)
    {
        var blobClient = await GetBlobClient(applicationId, blobId);
        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<string> GetBlobUri(Guid applicationId, Guid blobId)
    {
        var blobClient = await GetBlobClient(applicationId, blobId);
        return blobClient.Uri.AbsoluteUri;
    }

    public async Task Delete(Guid applicationId, Guid blobId)
    {
        var blobClient = await GetBlobClient(applicationId, blobId);
        await blobClient.DeleteIfExistsAsync();
    }

    private async Task<BlobContainerClient> GetContainerClient(Guid applicationId, bool isPublicAccess = false)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(applicationId.ToString());
        await containerClient.CreateIfNotExistsAsync(
            isPublicAccess ? PublicAccessType.BlobContainer : PublicAccessType.None
        );
        return containerClient;
    }

    private async Task<BlobClient> GetBlobClient(Guid applicationId, Guid blobId)
    {
        var containerClient = await GetContainerClient(applicationId);
        return containerClient.GetBlobClient(blobId.ToString());
    }
}