using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class ListContent
{
    public required List<string> Items { get; set; }
    public BodyListStyle Style { get; set; } = BodyListStyle.Bulleted;
}