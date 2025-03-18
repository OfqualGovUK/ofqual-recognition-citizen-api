namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class TaskSection : ITaskSection
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; }
    public int OrderNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    
    public readonly ICollection<TaskItem> TaskItems = new List<TaskItem>();
}