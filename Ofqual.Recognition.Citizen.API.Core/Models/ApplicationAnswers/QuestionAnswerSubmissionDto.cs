using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents an answer submitted for a task question.
/// </summary>
public class QuestionAnswerSubmissionDto : IApplicationAnswer
{
    public string Answer { get; set; }
}