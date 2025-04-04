using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class QuestionRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public QuestionRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_NextQuestionUrl()
    {
        // Initialise test container and connection
        await using var conection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(conection);

        // Arrange
        var section = await CreateTestSection(unitOfWork);
        var task = await CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await CreateTestQuestionType(unitOfWork);

        var question1 = await CreateTestQuestion(unitOfWork, task.TaskId, questionType.QuestionTypeId, 1,
            "review-submit/review-your-application", "{\"title\":\"test.\"}");
        
        var question2 = await CreateTestQuestion(unitOfWork, task.TaskId, questionType.QuestionTypeId, 2,
            "declaration-and-submit/submit", "{\"title\":\"test1.\"}");
        
        unitOfWork.Commit();
        
        // Act
        var result = await unitOfWork.QuestionRepository.GetNextQuestionUrl(question1.QuestionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question2.QuestionURL, result!.NextQuestionUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Null_If_No_Next_Question()
    {
        // Initialise test container and connection
        await using var conection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(conection);

        // Arrange
        var section = await CreateTestSection(unitOfWork);
        var task = await CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await CreateTestQuestionType(unitOfWork);

        var lastQuestion = await CreateTestQuestion(unitOfWork, task.TaskId, questionType.QuestionTypeId, 2,
            "last-question", "{\"title\":\"End.\"}");

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetNextQuestionUrl(lastQuestion.QuestionId);

        // Assert
        Assert.Null(result);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Null_If_Question_Does_Not_Exist()
    {
        // Initialise test container and connection
        await using var conection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(conection);

        // Act
        var result = await unitOfWork.QuestionRepository.GetNextQuestionUrl(Guid.NewGuid());

        // Assert
        Assert.Null(result);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    private async Task<Section> CreateTestSection(UnitOfWork unitOfWork)
    {
        var section = new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@domain.com"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Section]
            (SectionId, SectionName, OrderNumber, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@SectionId, @SectionName, @SectionOrderNumber, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            section,
            unitOfWork.Transaction);

        return section;
    }

    private async Task<TaskItem> CreateTestTask(UnitOfWork unitOfWork, Guid sectionId)
    {
        var task = new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskOrderNumber = 1,
            SectionId = sectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@domain.com"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Task]
            (TaskId, TaskName, OrderNumber, SectionId, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@TaskId, @TaskName, @TaskOrderNumber, @SectionId, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            task,
            unitOfWork.Transaction);

        return task;
    }

    private async Task<QuestionType> CreateTestQuestionType(UnitOfWork unitOfWork)
    {
        var questionType = new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@domain.com"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[QuestionType]
            (QuestionTypeId, QuestionTypeName, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionTypeId, @QuestionTypeName, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            questionType,
            unitOfWork.Transaction);

        return questionType;
    }

    private async Task<Question> CreateTestQuestion(UnitOfWork unitOfWork, Guid taskId, Guid questionTypeId, int order, string url, string content)
    {
        var question = new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = taskId,
            QuestionOrderNumber = order,
            QuestionTypeId = questionTypeId,
            QuestionContent = content,
            QuestionURL = url,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@domain.com"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Question]
            (QuestionId, TaskId, OrderNumber, QuestionTypeId, QuestionContent, QuestionURL, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionId, @TaskId, @QuestionOrderNumber, @QuestionTypeId, @QuestionContent, @QuestionURL, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            question,
            unitOfWork.Transaction);

        return question;
    }
}
