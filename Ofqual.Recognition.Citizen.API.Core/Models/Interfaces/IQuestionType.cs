using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

public interface IQuestionType
{
    public QuestionTypeEnum? QuestionType { get; set; }
    public string? QuestionTypeName { get; set; }
}