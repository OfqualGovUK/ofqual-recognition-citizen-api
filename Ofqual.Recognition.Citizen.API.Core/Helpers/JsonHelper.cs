using System.Text.Json;

namespace Ofqual.Recognition.Citizen.API.Core.Helpers;

public static class JsonHelper
{
    public static bool IsEmptyJsonObject(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object && !doc.RootElement.EnumerateObject().Any();
        }
        catch (JsonException)
        {
            return false;
        }
    }
}