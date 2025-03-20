using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a section containing a list of tasks with their statuses.
/// </summary>
public class TaskItemStatusSectionDto : ISection
{
    public Guid SectionId { get; set; }
    public required string SectionName { get; set; }

    public IEnumerable<ITaskItemStatus> Tasks { get; set; } = new List<ITaskItemStatus>();
}