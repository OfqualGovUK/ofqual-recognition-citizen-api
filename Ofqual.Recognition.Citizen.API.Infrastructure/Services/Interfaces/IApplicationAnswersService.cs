using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.ApplicationAnswers;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IApplicationAnswersService
{
    public Task<bool> SavePreEngagementAnswers(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers);
    public Task<List<QuestionAnswerTaskSectionDto>> GetTaskAnswerReview(Guid applicationId, Guid taskId);
    public Task<ValidationResponse?> ValidateQuestionAnswers(Guid questionId, string answerJson);
    public Task<List<ApplicationReviewSectionDto>> GetAllApplicationAnswerReview(Guid applicationId);
}