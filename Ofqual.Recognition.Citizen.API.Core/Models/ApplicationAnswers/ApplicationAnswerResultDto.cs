
namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents the result of submitting an application answer, including the next question URL if available.
/// </summary>
public class ApplicationAnswerResultDto
{
    public string? NextQuestionUrl { get; set; }
}