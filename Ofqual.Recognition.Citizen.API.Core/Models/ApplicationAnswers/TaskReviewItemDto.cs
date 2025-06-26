namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a question and its answer.
/// </summary>
public class TaskReviewItemDto
{
    public string? QuestionText { get; set; }
    public List<string>? AnswerValue { get; set; }
    public string? QuestionUrl { get; set; }
}