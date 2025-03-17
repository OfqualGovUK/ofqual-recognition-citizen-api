namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class TaskSectionDto
{
    public Guid SectionId { get; set; }
    public string SectionName { get; set; }
    public int OrderNumber { get; set; } 
    public List<TaskStatusDto> Tasks { get; set; } = new List<TaskStatusDto>();
}