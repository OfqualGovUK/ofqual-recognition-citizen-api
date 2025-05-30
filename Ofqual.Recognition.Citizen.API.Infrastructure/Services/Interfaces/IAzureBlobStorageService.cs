namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IAzureBlobStorageService
{
    public Task Write(Guid applicationId, Guid blobId, Stream stream, bool isPublicAccess = false);
    public Task<Stream> Read(Guid applicationId, Guid blobId);
    public Task<string> GetBlobUri(Guid applicationId, Guid blobId);
    public Task Delete(Guid applicationId, Guid blobId);
}