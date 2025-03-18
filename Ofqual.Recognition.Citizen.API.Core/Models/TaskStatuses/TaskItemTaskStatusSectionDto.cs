using Ofqual.Recognition.Citizen.API.Core.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

public class TaskItemTaskStatusSectionDto : ISection
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }

    public IEnumerable<ITaskItemStatus> Tasks { get; set; } = new List<ITaskItemStatus>();
}