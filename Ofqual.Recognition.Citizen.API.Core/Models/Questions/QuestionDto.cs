
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a question with its content, type and associated task name.
/// </summary>
public class QuestionDto : IQuestion, IQuestionType
{
    public required string TaskName { get; set; }
    public required string QuestionTypeName { get; set; }
    public required string QuestionContent { get; set; }
}