using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Tests.Unit.Services.Validation;

public partial class ApplicationAnswersServiceTests
{
    public class ApplicationServiceTestQuestionRepository : IQuestionRepository
    {

        private readonly QuestionDetails _questionDetails;
        private readonly bool _mockExist;

        public ApplicationServiceTestQuestionRepository(QuestionDetails questionDetails, bool mockExist = false)
        {
            _questionDetails = questionDetails;
            _mockExist = mockExist;
        }

        public Task<QuestionDetails?> GetQuestion(Guid taskId, Guid questionId) =>
            Task.FromResult<QuestionDetails?>(_questionDetails);

        public Task<bool> CheckIfQuestionAnswerExists(Guid questionId, Guid taskId, string questionItemName, string questionItemAnswer) =>
            Task.FromResult(_mockExist);


        public Task<IEnumerable<Question>> GetAllQuestions()
        {
            throw new NotImplementedException();
        }

        public Task<QuestionDetails?> GetQuestion(string taskNameUrl, string questionNameUrl)
        {
            throw new NotImplementedException();
        }



        public Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<TaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer)
        {
            throw new NotImplementedException();
        }
    }
}

