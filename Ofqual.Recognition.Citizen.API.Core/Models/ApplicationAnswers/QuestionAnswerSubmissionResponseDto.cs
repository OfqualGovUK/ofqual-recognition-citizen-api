
namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Returned after submitting an answer, containing the next question URL for redirection.
/// </summary>
public class QuestionAnswerSubmissionResponseDto
{
    public string? NextQuestionUrl { get; set; }
}