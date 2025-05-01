
namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents the saved answer for a specific question.
/// </summary>
public class QuestionAnswerDto
{
    public Guid QuestionId { get; set; }
    public string? Answer { get; set; }
}