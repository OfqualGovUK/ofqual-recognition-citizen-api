namespace Ofqual.Recognition.API.Models.JSON.Questions;

public class ValidationRule
{
    /// <summary>
    /// Requires that an answer is provided
    /// * Checkboxes: A value will need to be selected
    /// * Input: A non-empty value is required
    /// </summary>
    public bool? Required { set; get; }

    /// <summary>
    /// Requires that this value for this question does not already exist in database
    /// </summary>
    public bool? Unique { set; get; }

    /// <summary>
    /// Modifies MinLength and MaxLength properties to check number of words and not characters
    /// Ignored when working with non text input components
    /// </summary>
    public bool? CountWords { set; get; }

    /// <summary>
    /// Require that a text input has a minimum number of characters
    /// Ignored when working with non text input components
    /// </summary>
    public int? MinLength { set; get; }

    /// <summary>
    /// Require that a text input contains no more than the specified number of characters
    /// Ignored when working with non text input components
    /// </summary>
    public int? MaxLength { set; get; }

    /// <summary>
    /// Requires that the minimum number of checkbox items is selected 
    /// Ignored by text inputs
    /// </summary>
    public int? MinSelected { set; get; }

    /// <summary>
    /// Requires that no more than the specified number of checkbox items is selected 
    /// Ignored by text inputs
    /// </summary>
    public int? MaxSelected { set; get; }

    /// <summary>
    /// Specifies a Regex pattern to be applied to text input fields
    /// Ignored by text inputs
    /// </summary>
    public string? Pattern { set; get; }
}
