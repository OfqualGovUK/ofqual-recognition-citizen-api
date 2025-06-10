using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationAnswersRepository
{
    public Task<IEnumerable<TaskQuestionAnswer>?> GetAllApplicationAnswers(Guid applicationId);
    public Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer);
    public Task<IEnumerable<TaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId);
    public Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId);
}