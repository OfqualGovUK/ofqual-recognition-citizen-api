namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class TaskItem : ITaskItem
{
    public Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public Guid SectionId { get; set; }
    public int OrderNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn  { get; set; }
    public string? ModifiedByUpn  { get; set; }

    public readonly ICollection<Question> Questions = new List<Question>();
}