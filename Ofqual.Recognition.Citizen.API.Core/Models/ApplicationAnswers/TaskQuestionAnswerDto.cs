namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Combines task, question and the related answer for an application.
/// </summary>
public class TaskQuestionAnswerDto
{
    // Task table
    public Guid TaskId { get; set; }
    public string TaskName { get; set; }
    public int TaskOrder { get; set; }

    // Question table
    public Guid QuestionId { get; set; }
    public string QuestionContent { get; set; }
    public string QuestionUrl { get; set; }

    // ApplicationAnswers table
    public string? Answer { get; set; }
}
