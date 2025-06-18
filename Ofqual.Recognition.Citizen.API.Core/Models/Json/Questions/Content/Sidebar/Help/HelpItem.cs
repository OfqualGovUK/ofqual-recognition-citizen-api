namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class HelpItem
{
    /// <summary>
    /// A list of links included in this help item.
    /// </summary>
    public List<Link>? Links { get; set; }

    /// <summary>
    /// A list of content blocks, such as a heading or paragraph.
    /// </summary>
    public List<BodyItem>? Content { get; set; }
}