using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface ICheckYourAnswersService
{
   public List<QuestionAnswerReviewDto> GetQuestionAnswers(IEnumerable<TaskQuestionAnswerDto> questions);
}