using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository TaskRepository { get; }
    IApplicationRepository ApplicationRepository { get; }
    IQuestionRepository QuestionRepository { get; }
    
    void Commit();
    void Rollback();
}
