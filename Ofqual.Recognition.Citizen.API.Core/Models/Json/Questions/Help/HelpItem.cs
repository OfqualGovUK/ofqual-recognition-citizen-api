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

    /// <summary>
    /// Returns true if this help item contains any links.
    /// </summary>
    public bool HasLinks => Links != null && Links.Count != 0;
    
    /// <summary>
    /// Returns true if this help item contains any content.
    /// </summary>
    public bool HasContent => Content != null && Content.Count != 0;
}