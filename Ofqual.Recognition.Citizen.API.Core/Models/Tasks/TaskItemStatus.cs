using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class TaskItemStatus : ITaskItemStatus
{
    public Guid TaskStatusId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid TaskId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public TaskStatusEnum Status { get; set; }
    public string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
}