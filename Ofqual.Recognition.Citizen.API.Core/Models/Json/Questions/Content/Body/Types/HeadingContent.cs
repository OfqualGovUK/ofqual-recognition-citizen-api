using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class HeadingContent
{
    public required string Text { get; set; }
    public BodyTextSize Size { get; set; } = BodyTextSize.L;
}
