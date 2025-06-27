namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a section in the application review.
/// </summary>
public class TaskReviewSectionDto
{
    public required string SectionName { get; set; }
    public List<TaskReviewGroupDto> TaskGroups { get; set; } = new List<TaskReviewGroupDto>();
}