namespace Ofqual.Recognition.Citizen.API.Core.Models
{
    public class AzureBlobStorageConfiguration
    {
        public string? ConnectionString;
        public string? ServiceUri;
        public bool UseManagedIdentity { get; set; } = false;
    }
}
