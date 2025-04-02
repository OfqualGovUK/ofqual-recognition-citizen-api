namespace Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

public interface IQuestion
{
    public Guid QuestionId { get; set; }
    public string QuestionContent { get; set; }
}

