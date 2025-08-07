using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IApplicationAnswersRepository
{
    public Task<IEnumerable<SectionTaskQuestionAnswer>?> GetAllApplicationAnswers(Guid applicationId);
    public Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer, string upn);
    public Task<IEnumerable<SectionTaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId);
    public Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId);
    public Task<bool> CheckIfQuestionAnswerExists(Guid questionId, string questionItemName, string questionItemAnswer, Guid? applicationId);
}