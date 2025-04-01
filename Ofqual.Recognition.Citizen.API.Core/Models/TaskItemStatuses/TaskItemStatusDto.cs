using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a task with its status and first question URL for redirection.
/// </summary>
public class TaskItemStatusDto : ITaskItemStatus
{
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public TaskStatusEnum Status { get; set; }
    public required string FirstQuestionUrl { get; set; }
}