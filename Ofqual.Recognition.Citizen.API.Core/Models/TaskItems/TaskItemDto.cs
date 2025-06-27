using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents task details within a section.
/// </summary>
public class TaskItemDto : ITaskItem
{
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public required string TaskNameUrl { get; set; }
    public required IEnumerable<TaskStage> TaskStages { get; set; }
    public int TaskOrderNumber { get; set; }
    public Guid SectionId { get; set; }
}