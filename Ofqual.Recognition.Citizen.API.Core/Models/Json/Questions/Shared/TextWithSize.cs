namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class TextWithSize
{
    /// <summary>
    /// The text content to be displayed.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The display size of the text, such as "s", "m", "l".
    /// </summary>
    public string Size { get; set; } = "m";
}