namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class CheckBoxItem
{
    /// <summary>
    /// The text label shown to the user for this checkbox.
    /// </summary>
    public required string Label { get; set; }
    
    /// <summary>
    /// The value sent when this checkbox is selected.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// A list of select dropdowns that appear when this checkbox is selected.
    /// </summary>
    public List<Select>? ConditionalSelects { get; set; }

    /// <summary>
    /// A list of text inputs that appear when this checkbox is selected.
    /// </summary>
    public List<TextInputItem>? ConditionalInputs { get; set; }
}