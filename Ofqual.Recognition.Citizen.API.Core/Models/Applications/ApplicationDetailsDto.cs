using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents the users application details.
/// </summary>
public class ApplicationDetailsDto : IApplication
{
    public Guid ApplicationId { get; set; }
    public Guid OwnerUserId { get; set; }
    public bool Submitted { get; set; }
}