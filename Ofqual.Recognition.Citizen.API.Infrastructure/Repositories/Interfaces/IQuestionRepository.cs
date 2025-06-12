
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    public Task<QuestionDetails?> GetQuestionByTaskAndQuestionUrl(string taskNameUrl, string questionNameUrl);
    public Task<QuestionDetails?> GetQuestionByQuestionId(Guid questionId);
    public Task<IEnumerable<Question>> GetAllQuestions();
}