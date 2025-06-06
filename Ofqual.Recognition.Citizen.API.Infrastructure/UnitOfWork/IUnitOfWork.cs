using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using System.Data;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public interface IUnitOfWork : IDisposable
{
    public ITaskRepository TaskRepository { get; }
    public IApplicationRepository ApplicationRepository { get; }
    public IQuestionRepository QuestionRepository { get; }
    public IAttachmentRepository AttachmentRepository { get; }

    public IDbConnection Connection { get; }
    public IDbTransaction Transaction { get; }

    public void Commit();
    public void Rollback();
}
