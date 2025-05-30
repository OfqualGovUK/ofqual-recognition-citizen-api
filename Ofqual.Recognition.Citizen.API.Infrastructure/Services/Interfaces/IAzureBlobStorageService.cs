namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IAzureBlobStorageService
{
    public Task Write(Guid blobId, Stream stream, bool isPublicAccess = false);
    public Task<Stream> Read(Guid blobId);
    public Task<string> GetBlobUri(Guid blobId);
    public Task Delete(Guid blobId);
}