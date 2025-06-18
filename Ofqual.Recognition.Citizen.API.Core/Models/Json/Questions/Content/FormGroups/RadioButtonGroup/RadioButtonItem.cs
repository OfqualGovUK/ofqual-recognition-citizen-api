namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class RadioButtonItem
{
    /// <summary>
    /// The text label shown to the user for this radio button.
    /// </summary>
    public required string Label { get; set; }

    /// <summary>
    /// The value sent when this radio button is selected.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// Hint text shown for this specific radio button item.
    /// </summary>
    public string? Hint { get; set; }

    /// <summary>
    /// A list of select dropdowns that appear when this radio button is selected.
    /// </summary>
    public List<Select>? ConditionalSelects { get; set; }

    /// <summary>
    /// A list of text inputs that appear when this radio button is selected.
    /// </summary>
    public List<TextInputItem>? ConditionalInputs { get; set; }
}
