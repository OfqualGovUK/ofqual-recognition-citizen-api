using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class BodyItem
{
    public required BodyItemType Type { get; set; }
    public HeadingContent? HeadingContent { get; set; }
    public ParagraphContent? ParagraphContent { get; set; }
    public ListContent? ListContent { get; set; }
    public ButtonContent? ButtonContent { get; set; }
}
