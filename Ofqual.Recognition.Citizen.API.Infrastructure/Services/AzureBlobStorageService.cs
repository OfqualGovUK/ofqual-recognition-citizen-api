using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class AzureBlobStorageService : IAzureBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorageService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<bool> Write(Guid applicationId, Guid blobId, Stream stream, bool isPublicAccess = false)
    {
        try
        {
            var containerClient = await GetContainerClient(applicationId, isPublicAccess);
            var blobClient = containerClient.GetBlobClient(blobId.ToString());
            await blobClient.UploadAsync(stream, overwrite: true);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upload blob. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
            return false;
        }
    }

    public async Task<Stream?> Read(Guid applicationId, Guid blobId)
    {
        try
        {
            var blobClient = await GetBlobClient(applicationId, blobId);
            var exists = await blobClient.ExistsAsync();

            if (!exists.Value)
            {
                Log.Warning("Blob not found when attempting to read. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
                return null;
            }

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read blob. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
            return null;
        }
    }

    public async Task<string?> GetBlobUri(Guid applicationId, Guid blobId)
    {
        try
        {
            var blobClient = await GetBlobClient(applicationId, blobId);
            var exists = await blobClient.ExistsAsync();

            if (!exists.Value)
            {
                Log.Warning("Blob not found when retrieving URI. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
                return null;
            }

            return blobClient.Uri.AbsoluteUri;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve blob URI. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
            return null;
        }
    }
    
    public async Task<bool> Delete(Guid applicationId, Guid blobId)
    {
        try
        {
            var blobClient = await GetBlobClient(applicationId, blobId);
            var response = await blobClient.DeleteIfExistsAsync();

            if (response.Value)
            {
                Log.Information("Blob deleted successfully. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
            }
            else
            {
                Log.Warning("Blob not found or already deleted. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete blob. Application ID: {ApplicationId}, Blob ID: {BlobId}", applicationId, blobId);
            return false;
        }
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