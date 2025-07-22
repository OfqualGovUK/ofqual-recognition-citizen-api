using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class CheckBoxGroup : IValidatable, ISectionGroup
{
    /// <summary>
    /// The heading shown above the checkboxes.
    /// </summary>
    public required TextWithSize Heading { get; set; }

    /// <summary>
    /// Gets the label text associated with the current object.
    /// </summary>
    string IValidatable.Label => Heading.Text;

    /// <summary>
    /// Hint text shown below the heading.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// A unique name used for the checkbox group.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// A list of selectable checkbox options.
    /// </summary>
    public required List<CheckBoxItem> Options { get; set; }

    /// <summary>
    /// The display name for the section shown on the review page.
    /// </summary>
    public string? SectionName { get; set; }
    
    /// <summary>
    /// Validation rules applied to the checkbox group.
    /// </summary>
    public ValidationRule? Validation { get; set; }
}