using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;


namespace Ofqual.Recognition.Citizen.API.Core.Models.QuestionType;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.QuestionType</c> database table,
/// defining question types and their audit tracking information.
/// </summary>
public class QuestionType : IQuestionType, IDataMetadata
{
    public Guid QuestionTypeId { get; set; }
    public required string QuestionTypeName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
}