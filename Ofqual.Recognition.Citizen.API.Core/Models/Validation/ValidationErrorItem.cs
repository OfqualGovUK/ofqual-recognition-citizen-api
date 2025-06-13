namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class ValidationErrorItem
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
