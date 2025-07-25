using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class Textarea : IValidatable, ISectionGroup
{
    /// <summary>
    /// The label displayed above the textarea.
    /// </summary>
    public required TextWithSize Label { get; set; }


    /// <summary>
    /// Gets the label text associated with the current object.
    /// </summary>
    string IValidatable.Label => Label.Text;


    /// <summary>
    /// Hint text displayed below the label.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// The name of the field.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The number of rows visible in the textarea.
    /// </summary>
    public int? Rows { get; set; } = 5;

    /// <summary>
    /// Enables or disables spellcheck.
    /// </summary>
    public bool? SpellCheck { get; set; } = true;

    /// <summary>
    /// The display name used on the review page.
    /// </summary>
    public string? SectionName { get; set; }

    /// <summary>
    /// Validation rules for the field.
    /// </summary>
    public ValidationRule? Validation { get; set; }
}
