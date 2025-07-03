using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using System.Data;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    public ITaskRepository TaskRepository { get; }
    public ITaskStatusRepository TaskStatusRepository { get; }
    public IApplicationRepository ApplicationRepository { get; }
    public IQuestionRepository QuestionRepository { get; }
    public IStageRepository StageRepository { get; }
    public IApplicationAnswersRepository ApplicationAnswersRepository { get; }
    public IAttachmentRepository AttachmentRepository { get; }
    public IUserRepository UserRepository { get; }
    
    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }

    public void Commit();
    public void Rollback();
}
