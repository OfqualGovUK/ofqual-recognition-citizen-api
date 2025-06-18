using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class TextWithSize
{
    /// <summary>
    /// The text content to be displayed.
    /// </summary>
    public required string Text { get; set; }

    /// <summary>
    /// The display size of the text.
    /// </summary>
    public BodyTextSize Size { get; set; } = BodyTextSize.M;
}