namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class TextInput
{
    /// <summary>
    /// The heading shown above the text inputs.
    /// </summary>
    public TextWithSize? Heading { get; set; }

    /// <summary>
    /// The collection of text input fields in the group.
    /// </summary>
    public required List<TextInputItem> TextInputs { get; set; }

    /// <summary>
    /// The display name for the section shown on the review page.
    /// </summary>
    public string? SectionName { get; set; }
}