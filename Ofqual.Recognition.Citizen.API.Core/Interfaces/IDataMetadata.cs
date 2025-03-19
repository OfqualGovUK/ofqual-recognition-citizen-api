namespace Ofqual.Recognition.Citizen.API.Core.Interfaces;

public interface IDataMetadata
{
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
}

