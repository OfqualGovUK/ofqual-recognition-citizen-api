namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Combines task, question and the related answer for an application.
/// </summary>
public class TaskQuestionAnswer
{
    // Task table
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public required string TaskNameUrl { get; set; }
    public int TaskOrder { get; set; }

    // Question table
    public Guid QuestionId { get; set; }
    public required string QuestionContent { get; set; }
    public required string QuestionNameUrl { get; set; }

    // ApplicationAnswers table
    public string? Answer { get; set; }
}
