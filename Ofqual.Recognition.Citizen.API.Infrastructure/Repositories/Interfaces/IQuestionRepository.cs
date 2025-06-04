
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    public Task<QuestionDetails?> GetQuestion(string taskNameUrl, string questionNameUrl);
    public Task<PreEngagementQuestionDetails?> GetPreEngagementQuestion(string taskNameUrl, string questionNameUrl);
    public Task<PreEngagementQuestionDto?> GetFirstPreEngagementQuestion();
    public Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer);
    public Task<IEnumerable<TaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId);
    public Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId);
    public Task<IEnumerable<Question>> GetAllQuestions();
}