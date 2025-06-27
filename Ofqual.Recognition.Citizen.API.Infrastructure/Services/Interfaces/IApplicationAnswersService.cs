using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.ApplicationAnswers;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IApplicationAnswersService
{
    public Task<bool> SubmitAnswerAndUpdateStatus(Guid applicationId, Guid taskId, Guid questionId, string answerJson);
    public Task<bool> SavePreEngagementAnswers(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers);
    public Task<List<TaskReviewSectionDto>> GetAllApplicationAnswerReview(Guid applicationId);
    public Task<List<TaskReviewGroupDto>> GetTaskAnswerReview(Guid applicationId, Guid taskId);
    public Task<ValidationResponse?> ValidateQuestionAnswers(Guid questionId, string answerJson);
    public Task<List<ApplicationReviewSectionDto>> GetAllApplicationAnswerReview(Guid applicationId);
}