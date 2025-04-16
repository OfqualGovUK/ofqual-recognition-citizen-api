
namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Returned after submitting an answer, containing the next question URL for redirection.
/// </summary>
public class QuestionAnswerSubmissionResponseDto
{
    public required string NextTaskNameUrl { get; set; }
    public required string NextQuestionNameUrl { get; set; }
}