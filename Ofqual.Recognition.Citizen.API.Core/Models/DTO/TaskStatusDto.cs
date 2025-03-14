using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class TaskStatusDto
{
    public Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public Guid SectionId { get; set; }
    public int OrderNumber { get; set; }
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.NotStarted;
    public DateTime TaskStatusCreatedDate { get; set; }
    public DateTime TaskStatusModifiedDate { get; set; }
}