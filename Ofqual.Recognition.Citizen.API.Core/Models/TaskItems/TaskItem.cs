using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.Task</c> database table,
/// defining tasks within a section, their order and audit tracking information.
/// </summary>
public class TaskItem : ITaskItem, IDataMetadata
{
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public int TaskOrderNumber { get; set; }
    public Guid SectionId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn  { get; set; }
    public string? ModifiedByUpn  { get; set; }

    public readonly ICollection<Question> Questions = new List<Question>();
}