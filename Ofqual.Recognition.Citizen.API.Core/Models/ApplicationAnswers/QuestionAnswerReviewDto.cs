namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Used for displaying a question and its answer on a review page.
/// </summary>
public class QuestionAnswerTaskReviewDto
{
    public string? QuestionText { get; set; }
    public List<string>? AnswerValue { get; set; }
    public string? QuestionUrl { get; set; }
}