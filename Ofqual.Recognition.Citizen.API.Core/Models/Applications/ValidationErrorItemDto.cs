namespace Ofqual.Recognition.Citizen.API.Core.Models.Applications;
public class ValidationErrorItemDto
{
    public string Property { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
}

public class ValidationErrorItemDtoCollection : List<ValidationErrorItemDto>   
{ 
    public ValidationErrorItemDtoCollection() 
    { 
        
    }   
}

