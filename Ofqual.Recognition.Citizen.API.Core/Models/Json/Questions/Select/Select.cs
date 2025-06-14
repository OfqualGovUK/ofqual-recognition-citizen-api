using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class Select : IValidatable
{
    /// <summary>
    /// The label shown above the select dropdown.
    /// </summary>
    public string Label { get; set; } = "Item";

    /// <summary>
    /// Hint text shown below the label.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// The name and id attribute for the select element.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Whether the select element is disabled.
    /// </summary>
    public bool Disabled { get; set; } = false;

    /// <summary>
    /// A list of selectable options.
    /// </summary>
    public required List<SelectOption> Options { get; set; }

    /// <summary>
    /// Validation rules applied to the select dropdown.
    /// </summary>
    public ValidationRule? Validation { get; set; }
}