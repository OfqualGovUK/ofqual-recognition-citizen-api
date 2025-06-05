namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class PreEngagementQuestionDetailsDto
{
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public required string QuestionTypeName { get; set; }
    public required string QuestionContent { get; set; }
    public required string CurrentQuestionUrl { get; set; }
    public string? PreviousQuestionUrl { get; set; }
    public string? NextQuestionUrl { get; set; }
}