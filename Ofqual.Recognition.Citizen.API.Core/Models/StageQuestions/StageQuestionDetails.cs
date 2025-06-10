namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class StageQuestionDetails
{
    public Guid QuestionId { get; set; }
    public required string QuestionContent { get; set; }
    public Guid TaskId { get; set; }
    public required string CurrentQuestionNameUrl { get; set; }
    public required string CurrentTaskNameUrl { get; set; }
    public required string QuestionTypeName { get; set; }
    public string? NextQuestionNameUrl { get; set; }
    public string? NextTaskNameUrl { get; set; }
    public string? PreviousQuestionNameUrl { get; set; }
    public string? PreviousTaskNameUrl { get; set; }
}