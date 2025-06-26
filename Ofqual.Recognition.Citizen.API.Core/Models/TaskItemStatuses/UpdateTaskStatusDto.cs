using Ofqual.Recognition.Citizen.API.Core.Attributes;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents an update to a task's status.
/// </summary>
public class UpdateTaskStatusDto
{
    [ValidEnumValue(typeof(TaskStatusEnum), ErrorMessage = "The status provided is not valid.")]
    public TaskStatusEnum Status { get; set; }
}