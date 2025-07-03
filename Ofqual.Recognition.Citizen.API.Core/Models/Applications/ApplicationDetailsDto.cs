namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents the users application details.
/// </summary>
public class ApplicationDetailsDto
{
    public Guid ApplicationId { get; set; }
    public bool Submitted { get; set; }
}