using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Validations;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class UpdateTaskStatusDto
{
    [ValidEnumValue(typeof(TaskStatusEnum), ErrorMessage = "The status provided is not valid.")]
    public TaskStatusEnum Status { get; set; }
}