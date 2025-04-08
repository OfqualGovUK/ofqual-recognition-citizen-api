
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    public Task<QuestionDto?> GetQuestion(string questionURL);
    public Task<QuestionAnswerResultDto?> GetNextQuestionUrl(Guid currentQuestionId);

    public Task<bool> InsertQuestionAnswer(Guid applicationId, Guid questionId, string answer);
}