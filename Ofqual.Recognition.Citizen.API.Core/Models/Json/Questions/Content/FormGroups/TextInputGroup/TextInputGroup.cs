using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class TextInputGroup : ISectionGroup
{
    /// <summary>
    /// The heading displayed above the group of text inputs.
    /// </summary>
    public TextWithSize? Heading { get; set; }

    /// <summary>
    /// A list of text input fields to render.
    /// </summary>
    public required List<TextInputItem> Fields { get; set; }

    /// <summary>
    /// The section name shown on the review page.
    /// </summary>
    public string? SectionName { get; set; }
}
