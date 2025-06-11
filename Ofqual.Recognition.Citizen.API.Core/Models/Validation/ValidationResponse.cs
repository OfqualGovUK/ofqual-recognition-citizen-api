namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class ValidationResponse
{
    public string? Message { get; set; }
    public IEnumerable<ValidationErrorItem>? Errors { get; set; }
}
