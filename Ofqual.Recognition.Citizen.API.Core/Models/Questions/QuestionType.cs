namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class QuestionType
{
    public Guid QuestionTypeId { get; set; }
    public string QuestionTypeName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn  { get; set; }
    public string? ModifiedByUpn  { get; set; }
}