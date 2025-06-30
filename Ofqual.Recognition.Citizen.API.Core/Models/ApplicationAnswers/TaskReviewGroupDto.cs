namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a group of questions and answers within a task.
/// </summary>
public class TaskReviewGroupDto
{
    public string? SectionHeading { get; set; }
    public List<TaskReviewItemDto> QuestionAnswers { get; set; } = new List<TaskReviewItemDto>();
}