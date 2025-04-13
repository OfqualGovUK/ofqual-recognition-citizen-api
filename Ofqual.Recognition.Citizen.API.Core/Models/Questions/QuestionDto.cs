
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a question with its content and type
/// </summary>
public class QuestionDto : IQuestion, IQuestionType
{
    public required Guid QuestionId { get; set; }
    public required Guid TaskId { get; set; }
    public required string QuestionTypeName { get; set; }
    public required string QuestionContent { get; set; }
    public required string CurrentQuestionUrl { get; set; }
    public string? PreviousQuestionUrl { get; set; }
}