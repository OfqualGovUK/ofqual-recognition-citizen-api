namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a section of questions and answers in the review.
/// </summary>
public class QuestionAnswerSectionDto
{
    public string? SectionHeading { get; set; }
    public List<QuestionAnswerReviewDto> QuestionAnswers { get; set; } = new();
}