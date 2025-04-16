using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class TaskQuestion: IQuestion, IQuestionType
{
    // Question table
    public Guid QuestionId { get; set; }
    public required string CurrentQuestionNameUrl { get; set; }
    public required string QuestionContent { get; set; }
    public Guid TaskId { get; set; }
    public string? PreviousQuestionNameUrl { get; set; }

    // QuestionType table
    public required string QuestionTypeName { get; set; }

    // Task table
    public required string TaskNameUrl { get; set; }
}





