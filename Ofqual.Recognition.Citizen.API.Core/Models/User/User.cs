using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class User : IDataMetadata
{
    public Guid UserId { get; set; }
    public Guid B2CId { get; set; }
    public string? EmailAddress { get; set; }
    public string? DisplayName { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}