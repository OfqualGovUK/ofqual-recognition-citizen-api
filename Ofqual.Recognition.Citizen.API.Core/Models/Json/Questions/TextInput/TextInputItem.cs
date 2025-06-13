using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class TextInputItem : IValidatable
{
    /// <summary>
    /// The label shown above the input field.
    /// </summary>
    public string? Label { get; set; }
    
    /// <summary>
    /// Hint text shown below the label to guide the user.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// The name and id attribute for the input field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether the input is disabled.
    /// </summary>
    public bool Disabled { get; set; } = false;

    /// <summary>
    /// The input type, such as text, number or email.
    /// </summary>
    public string InputType { get; set; } = "text";

    /// <summary>
    /// Validation rules applied to the text input.
    /// </summary>
    public ValidationRule? Validation { get; set; }
}