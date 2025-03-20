using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.TaskStatus</c> database table,
/// containing task status details, tracking information and references to the associated application and task.
/// </summary>
public class TaskItemStatus : ITaskItemStatus, IDataMetadata
{
    public Guid TaskStatusId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public int TaskOrderNumber { get; set; }
    public TaskStatusEnum Status { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}