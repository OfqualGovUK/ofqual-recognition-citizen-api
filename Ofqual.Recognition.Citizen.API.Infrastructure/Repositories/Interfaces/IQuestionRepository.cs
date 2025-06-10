
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    public Task<QuestionDetails?> GetQuestion(string taskNameUrl, string questionNameUrl);
    public Task<QuestionDetails?> GetQuestion(Guid taskId, Guid questionId);
    public Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer);
    public Task<IEnumerable<TaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId);
    public Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId);
    public Task<bool> CheckIfQuestionAnswerExists(Guid questionId, Guid taskId, string questionItemName, string questionItemAnswer);
    public Task<IEnumerable<Question>> GetAllQuestions();
}
