namespace Ofqual.Recognition.Citizen.API.Core.Helpers;

public static class FileValidationHelper
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".csv", ".jpeg", ".jpg", ".png",
        ".xlsx", ".doc", ".docx", ".pdf",
        ".json", ".odt", ".rtf", ".txt"
    };

    public static bool IsAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }
}