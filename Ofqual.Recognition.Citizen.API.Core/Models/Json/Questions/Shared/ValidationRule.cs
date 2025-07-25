namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class ValidationRule
{
    
    /// <summary>
    /// Allows a custom error message to be provided and displayed on the front-end
    /// if not specified, this will default to using the parent component name
    /// </summary>
    public string? ValidationLabel { get; set; }
    /// <summary>
    /// Requires that an answer is provided.
    /// * Checkboxes: A value must be selected.
    /// * Input: A non-empty value is required.
    /// </summary>
    public bool? Required { get; set; }

    /// <summary>
    /// Requires that this value does not already exist in the database.
    /// </summary>
    public bool? Unique { get; set; }

    /// <summary>
    /// Validates MinLength and MaxLength by counting the number of words.
    /// Ignored for non-text input components.
    /// </summary>
    public bool? CountWords { get; set; }

    /// <summary>
    /// Requires a minimum number of characters or words (words only if CountWords is true).
    /// </summary>
    public int? MinLength { get; set; }

    /// <summary>
    /// Requires a maximum number of characters or words (words only if CountWords is true).
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Requires that at least this many checkbox items are selected.
    /// Ignored for text input components.
    /// </summary>
    public int? MinSelected { get; set; }

    /// <summary>
    /// Requires that no more than this many checkbox items are selected.
    /// Ignored for text input components.
    /// </summary>
    public int? MaxSelected { get; set; }
    
    /// <summary>
    /// Regex pattern to validate input format.
    /// Ignored for non-text input components.
    /// </summary>
    public string? Pattern { get; set; }
}