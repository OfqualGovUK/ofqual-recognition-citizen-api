using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class Select : IValidatable
{
    /// <summary>
    /// The text label displayed above the select dropdown.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// The label used in validation messages.
    /// </summary>
    public string ValidationLabel => Label;

    /// <summary>
    /// Hint text shown below the label.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// The field name for the select element.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Indicates whether the select dropdown is disabled.
    /// </summary>
    public bool Disabled { get; set; } = false;

    /// <summary>
    /// The list of options shown in the dropdown.
    /// </summary>
    public required List<SelectOption> Options { get; set; }

    /// <summary>
    /// Validation rules applied to the select dropdown.
    /// </summary>
    public ValidationRule? Validation { get; set; }
}
