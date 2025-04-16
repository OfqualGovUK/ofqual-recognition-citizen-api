using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

/// <summary>
/// Represents a domain-level model of the <c>recognitionCitizen.Question</c> database table
/// </summary>
public class Question : IQuestion, IDataMetadata
{
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public int QuestionOrderNumber { get; set; }
    public Guid QuestionTypeId { get; set; }
    public required string QuestionContent { get; set; }
    public required string QuestionNameUrl { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn  { get; set; }
    public string? ModifiedByUpn  { get; set; }
}