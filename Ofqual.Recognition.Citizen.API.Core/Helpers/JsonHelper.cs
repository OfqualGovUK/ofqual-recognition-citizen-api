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
        if (parent != null)
        {
            return GetString(parent, childProperty);
        }
        return null;
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

        return obj.Properties()
                  .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                  ?.Value;
    }

    public static bool IsEmptyJsonObject(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Object &&
                   !doc.RootElement.EnumerateObject().Any();
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static List<string> GetFlattenedStringValuesByKey(JObject? data, string key)
    {
        if (data == null)
        {
            return new List<string> { "Not provided" };
        }

        var token = data[key] ?? FindNestedValue(data, key);
        if (token == null)
        {
            return new List<string> { "Not provided" };
        }

        if (token.Type == JTokenType.Array)
        {
            return token
                .Values<string?>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToList();
        }
        
        return new List<string> { token.ToString() };
    }

    public static JToken? FindNestedValue(JToken? token, string key)
    {
        if (token == null)
        {
            return null;
        }

        if (token.Type == JTokenType.Object)
        {
            foreach (var property in token.Children<JProperty>())
            {
                if (property.Name == key)
                {
                    return property.Value;
                }

                var nestedResult = FindNestedValue(property.Value, key);
                if (nestedResult != null)
                {
                    return nestedResult;
                }
            }
        }
        else if (token.Type == JTokenType.Array)
        {
            foreach (var item in token)
            {
                var nestedResult = FindNestedValue(item, key);
                if (nestedResult != null)
                {
                    return nestedResult;
                }
            }
        }

        return null;
    }

    public static HashSet<string> GetStringSetFromToken(JToken? token)
    {
        if (token is JArray array)
        {
            return array
                .Values<string?>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value!)
                .ToHashSet();
        }

        if (token is JValue value && !string.IsNullOrWhiteSpace(value.ToString()))
        {
            return new HashSet<string> { value.ToString()! };
        }

        return new HashSet<string>();
    }
}