

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;

public class TextArea : IFormComponent
{
    public string? Heading { get; set; }

    public string? Name { get; set; }

    public string? Hint { get; set; }

    public ComponentValidation? Validation { get; set; }
}

