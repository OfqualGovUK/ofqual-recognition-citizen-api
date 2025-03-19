using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Interfaces;

public interface ITaskItemStatus : ITaskItem
{
    public Guid TaskStatusId { get; set; }
    public TaskStatusEnum Status { get; set; }
}