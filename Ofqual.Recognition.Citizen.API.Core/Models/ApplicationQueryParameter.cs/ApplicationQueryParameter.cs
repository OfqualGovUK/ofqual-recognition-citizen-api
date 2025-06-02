namespace Ofqual.Recognition.Citizen.API.Core.Models.ApplicationQueryParameter.cs;

/// <summary>
/// Represents the query parameters for the result of application get endpoint
/// </summary>
public class ApplicationQueryParameter
{
    public string? OrganisationName { get; set; }
    public string? LegalName { get; set; }
    public string? Acronym { get; set; }
    public string? Website { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? JobRole { get; set; }
}