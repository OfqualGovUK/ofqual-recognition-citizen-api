
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace Ofqual.Recognition.Citizen.API.Core.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum ScanStatus
{
    Ok,
    Found,
    Pending,
    Failed,
    Warning
}