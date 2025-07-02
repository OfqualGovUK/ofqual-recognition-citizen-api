using Ofqual.Recognition.Citizen.API.Core.Attributes;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents an update to a task's status.
/// </summary>
public class UpdateTaskStatusDto
{
    [ValidEnumValue(typeof(StatusType), ErrorMessage = "The status provided is not valid.")]
    public StatusType Status { get; set; }
}