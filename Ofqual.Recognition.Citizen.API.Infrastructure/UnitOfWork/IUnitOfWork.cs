using System.Data;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    ITaskRepository TaskRepository { get; }
    IApplicationRepository ApplicationRepository { get; }
    IQuestionRepository QuestionRepository { get; }
    IDbConnection Connection { get; }
    IDbTransaction Transaction { get; }

    void Commit();
    void Rollback();
}
