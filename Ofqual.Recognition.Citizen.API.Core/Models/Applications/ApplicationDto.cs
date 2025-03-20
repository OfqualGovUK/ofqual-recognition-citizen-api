using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents the users application details.
/// </summary>
public class ApplicationDto : IApplication
{
    public Guid ApplicationId { get; set; }
}