
namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class ApplicationAnswerResultDto
{
    /// <summary>
    /// The URL of the next question, or null if there are no more questions in the sequence.
    /// </summary>
    public string? NextQuestionUrl { get; set; }
}