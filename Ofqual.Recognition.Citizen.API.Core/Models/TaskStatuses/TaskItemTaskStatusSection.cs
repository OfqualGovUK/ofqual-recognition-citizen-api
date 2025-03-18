using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

public class TaskItemTaskStatusSection : ITaskItemStatus, ISection
{
    // From Section Table
    public Guid SectionId { get; set; }
    public string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }

    // From Task Table
    public Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public int TaskOrderNumber { get; set; }

    // From Task Status Table
    public Guid TaskStatusId { get; set; }
    public TaskStatusEnum Status { get; set; }
}