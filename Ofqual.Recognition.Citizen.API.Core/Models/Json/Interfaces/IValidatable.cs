using Ofqual.Recognition.API.Models.JSON.Questions;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

public interface IValidatable
{
    public string Name { set; get; }

    public string Label { get; }  
    public ValidationRule? Validation { get; set; }
}
