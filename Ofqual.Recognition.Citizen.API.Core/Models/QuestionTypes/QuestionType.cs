using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.QuestionType</c> database table
/// </summary>
public class QuestionType : IQuestionType, IDataMetadata
{
    public QuestionTypeEnum? QuestionType { get; set; }
    public Guid? QuestionTypeId { get; set; }
    public required string QuestionTypeName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
}