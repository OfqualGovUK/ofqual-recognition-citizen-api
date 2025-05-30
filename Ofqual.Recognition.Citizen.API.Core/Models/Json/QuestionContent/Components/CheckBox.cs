using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;

public class CheckBoxItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool? Selected { get; set; } = null;
}

public class CheckBoxConditionalInputItem
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string InputType { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;

}

public class CheckBoxConditionalSelectItem
{
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Hint { get; set; } = string.Empty;
    public bool Disabled { get; set; } = false;

    public IEnumerable<CheckBoxItem> ConditionalSelects { get; set; } = new List<CheckBoxItem>();
}

public class CheckBox : IComponent
{
    public string? Heading { get; set; }
    public string? Hint { get; set; }
    public string? Name { get; set; }
    public ComponentValidation? Validation { get; set; }
    IEnumerable<CheckBoxItem> CheckBoxes { get; set; } = new List<CheckBoxItem>();
}