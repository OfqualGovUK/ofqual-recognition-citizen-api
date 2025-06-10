
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    public Task<QuestionDetails?> GetQuestion(string taskNameUrl, string questionNameUrl);
    public Task<IEnumerable<Question>> GetAllQuestions();
}