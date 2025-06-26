namespace Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

public interface IQuestion
{
    public Guid QuestionId { get; set; }
    public Guid TaskId { get; set; }
    public string QuestionContent { get; set; }
}