using System.Data;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction _transaction;

    public ITaskRepository TaskRepository { get; private set; }
    public IApplicationRepository ApplicationRepository { get; private set; }
    public IQuestionRepository QuestionRepository { get; private set; }

    public UnitOfWork(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        if (_connection.State != ConnectionState.Open){
            _connection.Open();
        }

        _transaction = _connection.BeginTransaction();
        InitialiseRepositories();
    }

    private void InitialiseRepositories()
    {
        TaskRepository = new TaskRepository(_connection, _transaction);
        ApplicationRepository = new ApplicationRepository(_connection, _transaction);
        QuestionRepository = new QuestionRepository(_connection, _transaction);
    }

    public IDbConnection Connection => _connection;
    public IDbTransaction Transaction => _transaction;

    public void Commit()
    {
        if (_transaction == null){
            throw new InvalidOperationException("Transaction is not available.");
        }

        try
        {
            _transaction.Commit();
        }
        catch
        {
            _transaction.Rollback();
            throw;
        }
        finally
        {
            _transaction.Dispose();
            _transaction = _connection.BeginTransaction();
            InitialiseRepositories();
        }
    }

    public void Rollback()
    {
        if (_transaction == null)
        {
            return;
        }

        _transaction.Rollback();
        _transaction.Dispose();

        _transaction = _connection.BeginTransaction();
        InitialiseRepositories();
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        // _connection?.Dispose();
    }
}