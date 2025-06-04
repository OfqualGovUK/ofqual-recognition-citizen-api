namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class PreEngagementAnswerDto
{
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public required string AnswerJson { get; set; }
}