using Ofqual.Recognition.Citizen.API.Core.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.Section</c> database table,
/// defining sections, their order and audit tracking information.
/// </summary>
public class Section : ISection, IDataMetadata
{
    public Guid SectionId { get; set; }
    public required string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    
    public readonly ICollection<ITaskItem> TaskItems = new List<ITaskItem>();
}