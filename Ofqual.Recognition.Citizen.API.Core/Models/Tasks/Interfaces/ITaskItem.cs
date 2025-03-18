namespace Ofqual.Recognition.Citizen.API.Core.Models;

public interface ITaskItem
{
    Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public int OrderNumber { get; set; }
}