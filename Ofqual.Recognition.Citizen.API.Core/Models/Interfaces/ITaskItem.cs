namespace Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
public interface ITaskItem
{
    Guid TaskId { get; set; }
    public string TaskName { get; set; }
}