using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

public interface ITaskItemStatus : ITaskItem
{
    public TaskStatusEnum Status { get; set; }
}