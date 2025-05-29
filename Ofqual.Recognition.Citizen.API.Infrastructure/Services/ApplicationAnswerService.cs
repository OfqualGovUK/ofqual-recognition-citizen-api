using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.Applications;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;
public class ApplicationAnswerService: IApplicationAnswerService
{
    private readonly IQuestionRepository _questionRepository;

    public ApplicationAnswerService(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }


    public async Task<IEnumerable<ValidationErrorItemDto>> ValidateQuestionAnswers(string taskNameUrl, string questionNameUrl, QuestionAnswerSubmissionDto answer)
    {
        var errors = new List<ValidationErrorItemDto>();

        var questionDetails = await _questionRepository.GetQuestion(taskNameUrl, questionNameUrl);




        return errors;
    }
}

