using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;

public class TextInputItem
{
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;

    public ComponentValidation? Validation { get; set; }
}

public class TextInput : IFormComponent
{
    public string? Heading { get; set; }
    public string? Hint { get; set; }
    public string? Name { get; set; }

    public ComponentValidation? Validation { get; set; }
    public IEnumerable<TextInputItem> TextInputs { get; set; } = new List<TextInputItem>();
}
