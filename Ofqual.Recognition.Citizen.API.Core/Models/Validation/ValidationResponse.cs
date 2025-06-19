namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class ValidationResponse
{
    public IEnumerable<ValidationErrorItem>? Errors { get; set; }
}
