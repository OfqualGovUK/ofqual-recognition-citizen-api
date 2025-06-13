namespace Ofqual.Recognition.Citizen.API.Core.Helpers;

public static class StringHelper
{
    public static string CapitaliseFirstLetter(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }
        
        return char.ToUpperInvariant(input[0]) + input.Substring(1).ToLowerInvariant();
    }
}