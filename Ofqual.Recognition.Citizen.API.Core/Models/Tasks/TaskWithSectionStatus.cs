using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class TaskWithSectionStatus : ITaskItemStatus, ITaskSection, ITaskItem
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }

    public Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public int TaskOrderNumber { get; set; }
    
    public TaskStatusEnum Status { get; set; }

    int ITaskSection.OrderNumber { get => SectionOrderNumber; set => SectionOrderNumber = value; }
    int ITaskItem.OrderNumber { get => TaskOrderNumber; set => TaskOrderNumber = value; }
}