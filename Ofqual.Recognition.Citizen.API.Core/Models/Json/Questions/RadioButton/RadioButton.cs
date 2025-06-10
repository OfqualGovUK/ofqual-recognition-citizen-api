using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class RadioButton : IValidatable
{
    /// <summary>
    /// The heading shown above the radio buttons.
    /// </summary>
    public TextWithSize? Heading { get; set; }

    /// <summary>
    /// Hint text shown below the heading.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// Paragraph text shown below the heading.
    /// </summary>
    public string? Paragraph { get; set; }

    /// <summary>
    /// A unique name used for the group of radio buttons.
    /// </summary>
    public required string Name { get; set; }


    public string Label => "Radio Button";

    /// <summary>
    /// The display name for the section shown on the review page.
    /// </summary>
    public string? SectionName { get; set; }

    /// <summary>
    /// Validation rules applied to the radio buttons group.
    /// </summary>
    public ValidationRule? Validation { get; set; }

    /// <summary>
    /// A list of individual radio button items to render.
    /// </summary>
    public required List<RadioButtonItem> Radios { get; set; }
}