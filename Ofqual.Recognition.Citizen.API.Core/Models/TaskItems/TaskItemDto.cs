using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents task details within a section.
/// </summary>
public class TaskItemDto : ITaskItem
{
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public required string TaskNameUrl { get; set; }
    public int TaskOrderNumber { get; set; }
    public StageType Stage { get; set; }
    public Guid SectionId { get; set; }
}