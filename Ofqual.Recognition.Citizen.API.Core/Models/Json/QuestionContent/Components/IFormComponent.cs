using Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Json.QuestionContent.Components;

[JsonDerivedType(typeof(CheckBox), nameof(CheckBox))]
[JsonDerivedType(typeof(RadioButton), nameof(RadioButton))]
[JsonDerivedType(typeof(TextInput), nameof(TextInput))]
[JsonDerivedType(typeof(TextItems), nameof(TextItems))]
public interface IFormComponent
{
    public string? Heading { get; set; }
    public string? Hint { get; set; }
    public string? Name { get; set; }

    public ComponentValidation? Validation { get; set; }
}
