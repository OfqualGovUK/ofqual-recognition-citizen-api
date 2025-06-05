using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.Applications;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IApplicationAnswersService
{
    public Task<bool> SavePreEngagementAnswers(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers);
    public List<QuestionAnswerSectionDto> GetQuestionAnswers(IEnumerable<TaskQuestionAnswer> questions);
    public Task<IEnumerable<ValidationErrorItemDto>?> ValidateQuestionAnswers(Guid taskId, Guid questionId, QuestionAnswerSubmissionDto answerDto);
}