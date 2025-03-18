using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class Application
{
    public Guid ApplicationId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn  { get; set; }
    public string? ModifiedByUpn  { get; set; }
    
    public readonly ICollection<TaskItemStatus> TaskItemStatuses = new List<TaskItemStatus>();
}