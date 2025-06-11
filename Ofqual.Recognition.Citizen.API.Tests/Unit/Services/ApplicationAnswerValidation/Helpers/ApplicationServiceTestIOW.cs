using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using System.Data;

namespace Ofqual.Recognition.Citizen.API.Tests.Unit.Services.Validation;

public partial class ApplicationAnswersServiceTests
{
    public class ApplicationServiceTestIOW : IUnitOfWork
    {
        private readonly IQuestionRepository _questionRepository;
        public ApplicationServiceTestIOW(IQuestionRepository questionRepository)
        {
            _questionRepository = questionRepository;
        }

        public ITaskRepository TaskRepository => throw new NotImplementedException();

        public IApplicationRepository ApplicationRepository => throw new NotImplementedException();

        public IQuestionRepository QuestionRepository => _questionRepository;

        public IStageRepository StageRepository => throw new NotImplementedException();

        public IApplicationAnswersRepository ApplicationAnswersRepository => throw new NotImplementedException();

        public IAttachmentRepository AttachmentRepository => throw new NotImplementedException();

        public IDbConnection Connection => throw new NotImplementedException();

        public IDbTransaction Transaction => throw new NotImplementedException();

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Rollback()
        {
            throw new NotImplementedException();
        }
    }
}

