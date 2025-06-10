namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class StageQuestionDto
{
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public string CurrentTaskNameUrl { get; set; } = string.Empty;
    public string CurrentQuestionNameUrl { get; set; } = string.Empty;
}