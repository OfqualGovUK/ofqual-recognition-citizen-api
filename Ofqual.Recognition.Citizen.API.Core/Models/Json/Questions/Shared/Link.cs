namespace Ofqual.Recognition.API.Models.JSON.Questions;
public class Link
{
    /// <summary>
    /// The text that will be shown for the link.
    /// </summary>
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// The URL the link will go to.
    /// </summary>
    public string Url { get; set; } = string.Empty;
}