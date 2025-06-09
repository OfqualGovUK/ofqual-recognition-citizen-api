
namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;


public class TextItems : FormSection { }
public abstract class FormSection : IFormComponent
{
    public string? Heading { get ; set; }
    public string? Hint { get ; set ; }
    public string? Name { set { SectionName = value; } get { return SectionName; } }
    public ComponentValidation? Validation { get; set ; }

    public string? SectionName { get; set; }
    public List<IFormComponent> Components { set; get; } = new List<IFormComponent>();
}

