namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Combines section, task, question and the related answer for an application.
/// </summary>
public class SectionTaskQuestionAnswer
{
    // Section Table
    public Guid SectionId { get; set; }
    public required string SectionName { get; set; }
    public int SectionOrderNumber { get; set; }

    // Task table
    public Guid TaskId { get; set; }
    public required string TaskName { get; set; }
    public required string TaskNameUrl { get; set; }
    public int TaskOrderNumber { get; set; }

    // Question table
    public Guid QuestionId { get; set; }
    public required string QuestionContent { get; set; }
    public required string QuestionNameUrl { get; set; }

    // ApplicationAnswers table
    public string Answer { get; set; } = "{}";
    public Guid ApplicationId { get; set; }
}
