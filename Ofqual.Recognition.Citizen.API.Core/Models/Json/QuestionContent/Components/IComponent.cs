using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;

public interface IComponent
{
    public string? Heading { get; set; }
    public string? Hint { get; set; }
    public string? Name { get; set; }

    public ComponentValidation? Validation { get; set; }
}
