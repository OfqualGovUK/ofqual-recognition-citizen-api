namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class AzureBlobStorageOptions
{
    public required string ConnectionString { get; set; }
    public required string ContainerName { get; set; }
}