using Ofqual.Recognition.Citizen.API.Core.Models.Json.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Helpers;

/// <summary>
/// A Helper class for string operations, particularly for formatting and validation-related tasks.
/// </summary>
public static class StringHelper
{

    /// <summary>
    /// Capitalises the first letter of a string, leaving the rest of the string unchanged.
    /// </summary>
    /// <param name="input">string to capitalise</param>
    /// <returns>Original string with the first letter capitalised</returns>
    public static string? CapitaliseFirstLetter(this string? input) =>
        string.IsNullOrWhiteSpace(input)
            ? input
            : string.Concat(
                char.ToUpperInvariant(input[0]), 
                input.Length > 1
                    ? input.AsSpan(1).ToString()
                    : string.Empty);

    /// <summary>
    /// Obtains the label to display for validation messages, from a validatable component.
    /// </summary>
    /// <param name="component">The validatable component</param>
    /// <returns>The validationLabel if specified, else return the component name</returns>
    public static string GetValidationLabel(this IValidatable component) =>
        string.IsNullOrWhiteSpace(component.Validation?.ValidationLabel)
            ? component.Label.CapitaliseFirstLetter()!
            : component.Validation.ValidationLabel;
}