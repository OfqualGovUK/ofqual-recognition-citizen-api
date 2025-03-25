using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Validations;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents an update to a task's status.
/// </summary>
public class UpdateTaskStatusDto
{
    [ValidEnumValue(typeof(TaskStatusEnum), ErrorMessage = "The status provided is not valid.")]
    public TaskStatusEnum Status { get; set; }
}