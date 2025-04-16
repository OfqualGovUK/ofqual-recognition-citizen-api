using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.TaskStatus</c> database table
/// </summary>
public class TaskItemStatus : ITaskItemStatus, IDataMetadata
{
    public Guid TaskStatusId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid TaskId { get; set; }
    public TaskStatusEnum Status { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}