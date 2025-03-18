using Ofqual.Recognition.Citizen.API.Core.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class Section : ISection, IDataMetadata
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    
    public readonly ICollection<ITaskItem> TaskItems = new List<ITaskItem>();
}