using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.ApplicationAnswers</c> database table,
/// containing answers submitted for application questions.
/// </summary>
public class QuestionAnswer : IQuestionAnswer
{
    public Guid ApplicationAnswersId  { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid QuestionId { get; set; }
    public string Answer  { get; set; }
}