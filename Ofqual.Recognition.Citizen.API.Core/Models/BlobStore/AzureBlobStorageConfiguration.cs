namespace Ofqual.Recognition.Citizen.API.Core.Models
{
    public class AzureBlobStorageConfiguration
    {
        public string? ConnectionString;
        public Uri? ServiceUri;
        public bool UseManagedIdentity { get; set; } = false;
    }
}
