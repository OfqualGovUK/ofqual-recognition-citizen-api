using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;
public class CheckBox : IValidatable
{
    /// <summary>
    /// The heading shown above the checkboxes.
    /// </summary>
    public TextWithSize? Heading { get; set; }

    /// <summary>
    /// Hint text shown below the heading.
    /// </summary>
    public string? Hint { get; set; }

    public string Label => Heading?.Text ?? "Select";

    /// <summary>
    /// A unique name used for the checkbox group.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The display name for the section shown on the review page.
    /// </summary>
    public string? SectionName { get; set; }
    
    /// <summary>
    /// Validation rules applied to the checkbox group.
    /// </summary>
    public ValidationRule? Validation { get; set; }

    /// <summary>
    /// A list of individual checkbox items to render.
    /// </summary>
    public required List<CheckBoxItem> CheckBoxes { get; set; }
}