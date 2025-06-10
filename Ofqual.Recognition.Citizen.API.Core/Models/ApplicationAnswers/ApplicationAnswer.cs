using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.ApplicationAnswers</c> database table
/// </summary>
public class ApplicationAnswer : IApplicationAnswer, IDataMetadata
{
    public Guid ApplicationAnswersId  { get; set; }
    public Guid ApplicationId { get; set; }
    public Guid QuestionId { get; set; }
    public string Answer  { get; set; } = string.Empty;
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
}