namespace Ofqual.Recognition.Citizen.API.Core.Models;

public interface ITaskSection
{
    Guid SectionId { get; set; }
    string SectionName { get; set; }
    int OrderNumber { get; set; }
}