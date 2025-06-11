namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class ValidationErrorItemDto
{
    public string PropertyName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
