namespace Ofqual.Recognition.Citizen.API.Core.Interfaces;

public interface ITaskItem
{
    Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public int TaskOrderNumber { get; set; }
}