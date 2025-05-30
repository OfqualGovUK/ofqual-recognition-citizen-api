using Ofqual.Recognition.Citizen.API.Core.Models.Applications;
using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IApplicationAnswerService
{
    public Task<IEnumerable<ValidationErrorItemDto>?> ValidateQuestionAnswers(string taskNameUrl, string questionNameUrl, QuestionAnswerSubmissionDto answer);

}

