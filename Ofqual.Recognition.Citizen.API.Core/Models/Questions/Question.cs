namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class Question
{
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public int OrderNumber { get; set; }
    public Guid QuestionTypeId { get; set; }
    public string QuestionContent { get; set; }
    public string QuestionURL { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn  { get; set; }
    public string? ModifiedByUpn  { get; set; }
}