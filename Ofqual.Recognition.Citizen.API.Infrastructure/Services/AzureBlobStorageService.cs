using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly string _containerName;

    public AzureBlobStorageService(AzureBlobStorageOptions options)
    {
        _blobServiceClient = new BlobServiceClient(options.ConnectionString);
        _containerName = options.ContainerName;
    }

    public async Task Write(Guid blobId, Stream stream, bool isPublicAccess = false)
    {
        var containerClient = await GetContainerClient(isPublicAccess);
        var blobClient = containerClient.GetBlobClient(blobId.ToString());
        await blobClient.UploadAsync(stream, overwrite: true);
    }

    public async Task<Stream> Read(Guid blobId)
    {
        var blobClient = await GetBlobClient(blobId);
        var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<string> GetBlobUri(Guid blobId)
    {
        var blobClient = await GetBlobClient(blobId);
        return blobClient.Uri.AbsoluteUri;
    }

    public async Task Delete(Guid blobId)
    {
        var blobClient = await GetBlobClient(blobId);
        await blobClient.DeleteIfExistsAsync();
    }

    private async Task<BlobContainerClient> GetContainerClient(bool isPublicAccess = false)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        await containerClient.CreateIfNotExistsAsync(
            isPublicAccess ? PublicAccessType.BlobContainer : PublicAccessType.None
        );
        return containerClient;
    }

    private async Task<BlobClient> GetBlobClient(Guid blobId)
    {
        var containerClient = await GetContainerClient();
        return containerClient.GetBlobClient(blobId.ToString());
    }
}