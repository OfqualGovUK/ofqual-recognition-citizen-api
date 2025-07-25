using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;
using System.Data;

namespace Ofqual.Recognition.Citizen.API.Infrastructure;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction _transaction;

    public ITaskRepository TaskRepository { get; private set; } = null!; //supress warnings as InitialiseRepositories() will initialise these
    public ITaskStatusRepository TaskStatusRepository { get; private set; } = null!;
    public IApplicationRepository ApplicationRepository { get; private set; } = null!;
    public IQuestionRepository QuestionRepository { get; private set; } = null!;
    public IStageRepository StageRepository { get; private set; } = null!;
    public IApplicationAnswersRepository ApplicationAnswersRepository { get; private set; } = null!;
    public IAttachmentRepository AttachmentRepository { get; private set; } = null!;
    public IUserRepository UserRepository { get; private set; } = null!;

    public UnitOfWork(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        if (_connection.State != ConnectionState.Open)
        {
            _connection.Open();
        }

        _transaction = _connection.BeginTransaction();
        InitialiseRepositories();
    }

    private void InitialiseRepositories()
    {
        TaskRepository = new TaskRepository(_connection, _transaction);
        TaskStatusRepository = new TaskStatusRepository(_connection, _transaction);
        ApplicationRepository = new ApplicationRepository(_connection, _transaction);
        QuestionRepository = new QuestionRepository(_connection, _transaction);
        StageRepository = new StageRepository(_connection, _transaction);
        ApplicationAnswersRepository = new ApplicationAnswersRepository(_connection, _transaction);
        AttachmentRepository = new AttachmentRepository(_connection, _transaction);
        UserRepository = new UserRepository(_connection, _transaction);
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
        _connection?.Dispose();
    }
}