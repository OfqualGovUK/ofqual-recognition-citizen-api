using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Ofqual.Recognition.Citizen.API.Core.Helpers;

public static class JsonHelper
{
    public static string? GetString(JToken? token, string propertyName)
    {
        return GetProperty(token, propertyName)?.ToString();
    }

    public static string? GetNestedString(JToken? token, string parentProperty, string childProperty)
    {
        var parent = GetProperty(token, parentProperty) as JObject;
        return parent != null ? GetString(parent, childProperty) : null;
    }

    public static JArray? GetArray(JToken? token, string propertyName)
    {
        return GetProperty(token, propertyName) as JArray;
    }

    public static JObject? GetObject(JToken? token, string propertyName)
    {
        return GetProperty(token, propertyName) as JObject;
    }

    public static JToken? GetProperty(JToken? token, string propertyName)
    {
        if (token is not JObject obj)
        {
            return null;
        }

        return obj.Properties().FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))?.Value;
    }
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