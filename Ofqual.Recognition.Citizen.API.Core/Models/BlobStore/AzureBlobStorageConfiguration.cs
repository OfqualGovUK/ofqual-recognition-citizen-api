namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class AzureBlobStorageConfiguration
{
    public string? ConnectionString { get; set; }
    public string? ServiceUri { get; set; }
    public bool UseManagedIdentity { get; set; } = false;
}