using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

/// <summary>
/// Represents the status of a task.
/// </summary>
public class TaskItemStatusDto : ITaskItemStatus
{
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public int TaskOrderNumber { get; set; }

    public Guid TaskStatusId { get; set; }
    public TaskStatusEnum Status { get; set; }
}