using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.API.Infrastructure;
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
    public async Task Should_Return_Question_By_QuestionURL()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var q1 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork, task.TaskId, questionType.QuestionTypeId, 1, "q1", "{\"title\":\"q1\"}");
        var q2 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork, task.TaskId, questionType.QuestionTypeId, 2, "q2", "{\"title\":\"q2\"}");
        var q3 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork, task.TaskId, questionType.QuestionTypeId, 3, "q3", "{\"title\":\"q3\"}");

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestion(task.TaskNameUrl, "q2");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(q2.QuestionId, result!.QuestionId);
        Assert.Equal(q2.QuestionContent, result.QuestionContent);
        Assert.Equal(q2.TaskId, result.TaskId);
        Assert.Equal("q2", result.CurrentQuestionNameUrl);
        Assert.Equal("q1", result.PreviousQuestionNameUrl);
        Assert.Equal("q3", result.NextQuestionNameUrl);
        Assert.Equal(task.TaskNameUrl, result.TaskNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Previous_Question_Url_And_Null_Next_For_Last_Question()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var first = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork, task.TaskId, questionType.QuestionTypeId, 1, "first", "{\"title\":\"1\"}");
        var last = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork, task.TaskId, questionType.QuestionTypeId, 2, "last", "{\"title\":\"2\"}");

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestion(task.TaskNameUrl, "last");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(last.QuestionId, result!.QuestionId);
        Assert.Equal("first", result.PreviousQuestionNameUrl);
        Assert.Null(result.NextQuestionNameUrl);
        Assert.Equal("last", result.CurrentQuestionNameUrl);
        Assert.Equal(task.TaskNameUrl, result.TaskNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Null_If_QuestionURL_Not_Found()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestion("non-task", "non-question");

        // Assert
        Assert.Null(result);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Null_Previous_Url_For_First_Question()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var first = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork, task.TaskId, questionType.QuestionTypeId, 1, "first", "{\"title\":\"1\"}");
        var second = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork, task.TaskId, questionType.QuestionTypeId, 2, "second", "{\"title\":\"2\"}");

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestion(task.TaskNameUrl, "first");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("first", result.CurrentQuestionNameUrl);
        Assert.Null(result.PreviousQuestionNameUrl);
        Assert.Equal("second", result.NextQuestionNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_All_Inserted_Questions()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var question1 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "question-1",
            "{\"title\":\"first question\"}");
        var question2 = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            2,
            "question-2",
            "{\"title\":\"second question\"}");

        unitOfWork.Commit();

        // Act
        var result = (await unitOfWork.QuestionRepository.GetAllQuestions()).ToList();

        // Assert
        Assert.NotEmpty(result);
        Assert.Contains(result, q => q.QuestionId == question1.QuestionId);
        Assert.Contains(result, q => q.QuestionId == question2.QuestionId);

        Assert.All(result, q =>
        {
            Assert.NotEqual(Guid.Empty, q.QuestionId);
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionNameUrl));
            Assert.False(string.IsNullOrWhiteSpace(q.QuestionContent));
            Assert.NotEqual(default, q.CreatedDate);
        });

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}