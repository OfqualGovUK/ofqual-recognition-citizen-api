namespace Ofqual.Recognition.Citizen.API.Core.Interfaces;

public interface ISection
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }
}