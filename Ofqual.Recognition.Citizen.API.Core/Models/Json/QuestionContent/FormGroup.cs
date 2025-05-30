using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;


public class FormGroup
{
    public List<IComponent> Components { set; get; } = new List<IComponent>();
}

public class ComponentItem
{
    public string label { get; set; } = string.Empty;
    public string value { get; set; } = string.Empty;
}

