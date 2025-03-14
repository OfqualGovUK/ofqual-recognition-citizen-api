using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository TaskRepository { get; }
    IApplicationRepository ApplicationRepository { get; }
    
    void Commit();
    void Rollback();
}
