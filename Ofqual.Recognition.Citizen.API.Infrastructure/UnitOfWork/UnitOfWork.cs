using System.Data;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction _transaction;
    public ITaskRepository TaskRepository { get; }
    public IApplicationRepository ApplicationRepository { get; }
    public IQuestionRepository QuestionRepository { get; }

    public UnitOfWork(IDbConnection connection)
    {
        _connection = connection;
        _connection.Open();
        _transaction = _connection.BeginTransaction();

        // Initialise repositories
        TaskRepository = new TaskRepository(_transaction);
        ApplicationRepository = new ApplicationRepository(_transaction);
        QuestionRepository = new QuestionRepository(_transaction);
    }

    public void Commit()
    {
        try
        {
            _transaction.Commit();
            _transaction.Dispose();
            _transaction = _connection.BeginTransaction();
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
    }

    public void Rollback()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = _connection.BeginTransaction();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
    }
}