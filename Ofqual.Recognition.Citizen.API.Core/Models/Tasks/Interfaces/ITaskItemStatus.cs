using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public interface ITaskItemStatus
{
    Guid TaskId { get; set; }
    TaskStatusEnum Status { get; set; }
}