using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a task question, combining question, task and question type details.
/// </summary>
public class QuestionDetails: IQuestion, IQuestionType
{
    // Question table
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public required string QuestionContent { get; set; }
    public required string CurrentQuestionNameUrl { get; set; }
    public string? PreviousQuestionNameUrl { get; set; }
    public string? NextQuestionNameUrl { get; set; }
    public QuestionTypeEnum? QuestionType { get; set; }
    // QuestionType table
    public string? QuestionTypeName { get; set; }

    // Task table
    public required string TaskNameUrl { get; set; }
}