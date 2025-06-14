using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class Textarea : IValidatable
{
    /// <summary>
    /// The label shown above the text box.
    /// </summary>
    public TextWithSize? Label { get; set; }

    string IValidatable.Label => Label?.Text ?? "Textarea";

    /// <summary>
    /// Hint text shown below the label.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// The name of the field
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The display name for the section shown on the review page.
    /// </summary>
    public string? SectionName { get; set; }
    
    /// <summary>
    /// The number of rows shown in the text area.
    /// </summary>
    public int? Rows { get; set; } = 5;

    /// <summary>
    /// Whether to use the browser's spellcheck.
    /// </summary>
    public bool? SpellCheck { get; set; } = true;

    /// <summary>
    /// Validation rules for the text box.
    /// </summary>
    public ValidationRule? Validation { get; set; }
}