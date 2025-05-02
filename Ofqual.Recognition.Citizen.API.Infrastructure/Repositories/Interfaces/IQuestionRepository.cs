
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    public Task<TaskQuestion?> GetQuestion(string taskNameUrl, string questionNameUrl);
    public Task<QuestionAnswerSubmissionResponseDto?> GetNextQuestionUrl(Guid currentQuestionId);
    public Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer);
    public Task<IEnumerable<TaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId);
    public Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId);
}