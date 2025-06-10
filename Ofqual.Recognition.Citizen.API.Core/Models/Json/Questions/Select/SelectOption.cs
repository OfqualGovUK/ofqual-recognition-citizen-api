namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class SelectOption
{
    /// <summary>
    /// The text shown to the user.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// The value submitted in the form.
    /// </summary>
    public string? Value { get; set; }
    
    /// <summary>
    /// Whether this option is selected.
    /// </summary>
    public bool Selected { get; set; } = false;
}