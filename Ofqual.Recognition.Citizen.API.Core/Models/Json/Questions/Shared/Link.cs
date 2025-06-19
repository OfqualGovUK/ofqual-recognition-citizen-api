namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class Link
{
    /// <summary>
    /// The text that will be shown for the link.
    /// </summary>
    public required string Text { get; set; }
    
    /// <summary>
    /// The URL the link will go to.
    /// </summary>
    public required string Url { get; set; }
}