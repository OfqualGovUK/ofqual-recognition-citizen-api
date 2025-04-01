
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IQuestionRepository
{
    public Task<QuestionDto?> GetQuestion(string questionURL);
}