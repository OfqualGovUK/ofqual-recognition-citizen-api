using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository TaskRepository { get; }
    IApplicationRepository ApplicationRepository { get; }
    
    void Commit();
    void Rollback();
}
