using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class ParagraphContent
{
    public string? Text { get; set; }
    public BodyTextSize Size { get; set; } = BodyTextSize.M;
    public string? TextBeforeLink { get; set; }
    public string? TextAfterLink { get; set; }
    public Link? Link { get; set; }
}