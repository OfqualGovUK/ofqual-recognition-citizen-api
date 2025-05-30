namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IAzureBlobStorageService
{
    Task Write(Guid blobId, Stream stream, bool isPublicAccess = false);
    Task<Stream> Read(Guid blobId);
    Task<string> GetBlobUri(Guid blobId);
    Task Delete(Guid blobId);
}