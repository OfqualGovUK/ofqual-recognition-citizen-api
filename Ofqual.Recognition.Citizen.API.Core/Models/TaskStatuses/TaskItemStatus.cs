using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

public class TaskItemStatus : ITaskItemStatus, IDataMetadata
{
    public Guid TaskStatusId { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public int TaskOrderNumber { get; set; }
    public TaskStatusEnum Status { get; set; }
    public string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}