using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class RadioButtonGroup : IValidatable, ISectionGroup
{
    /// <summary>
    /// The heading shown above the radio button group.
    /// </summary>
    public required TextWithSize Heading { get; set; }

    /// <summary>
    /// The label used in validation messages.
    /// </summary>
    public string ValidationLabel => Heading.Text;
    
    /// <summary>
    /// Hint text displayed beneath the heading.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// Paragraph text displayed beneath the heading.
    /// </summary>
    public string? Paragraph { get; set; }

    /// <summary>
    /// The field name used for form submission and validation.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The list of individual radio button options.
    /// </summary>
    public required List<RadioButtonItem> Options { get; set; }

    /// <summary>
    /// The label used for this section on the review page.
    /// </summary>
    public string? SectionName { get; set; }

    /// <summary>
    /// Validation rules for the radio button group.
    /// </summary>
    public ValidationRule? Validation { get; set; }
}
