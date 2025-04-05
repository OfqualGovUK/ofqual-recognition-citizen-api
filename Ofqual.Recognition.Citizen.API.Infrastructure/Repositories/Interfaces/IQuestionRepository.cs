
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    Task<QuestionDto?> GetQuestion(string questionURL);
    Task<ApplicationAnswerResultDto?> GetNextQuestionUrl(Guid currentQuestionId);
}