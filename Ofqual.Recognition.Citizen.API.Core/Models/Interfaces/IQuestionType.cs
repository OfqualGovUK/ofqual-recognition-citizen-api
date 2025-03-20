namespace Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

public interface IQuestionType
{
    public Guid QuestionTypeId { get; set; }
    public string QuestionTypeName { get; set; }
}

