using System.Text.Json;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Helper;
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
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var expectedQuestion = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "test/question-url",
            "{\"title\":\"sample question\"}"
        );

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestion("test/question-url");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedQuestion.QuestionId, result!.QuestionId);
        Assert.Equal(expectedQuestion.QuestionContent, result.QuestionContent);
        Assert.Equal(expectedQuestion.TaskId, result.TaskId);
        Assert.Equal(questionType.QuestionTypeName, result.QuestionTypeName);

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
        var result = await unitOfWork.QuestionRepository.GetQuestion("non-existent-question-url");

        // Assert
        Assert.Null(result);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_NextQuestionUrl()
    {
        // Initialise test container and connection
        await using var conection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(conection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var question1 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task.TaskId, questionType.QuestionTypeId, 1,
            "review-submit/review-your-application", "{\"title\":\"test.\"}");

        var question2 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task.TaskId, questionType.QuestionTypeId, 2,
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
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var lastQuestion = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task.TaskId, questionType.QuestionTypeId, 2,
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Insert_Question_Answer_With_Json_Successfully()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var question = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "integration-test/question",
            "{\"title\":\"Insert test\"}"
        );

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);

        unitOfWork.Commit();

        var answerJson = JsonSerializer.Serialize(new { value = "This is a test answer." });

        // Act
        var success = await unitOfWork.QuestionRepository.InsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson
        );

        unitOfWork.Commit();

        // Assert
        Assert.True(success);

        var insertedAnswer = await QuestionTestDataBuilder.GetInsertedQuestionAnswer(
            unitOfWork,
            application.ApplicationId,
            question.QuestionId
        );

        Assert.NotNull(insertedAnswer);
        Assert.Equal(application.ApplicationId, insertedAnswer!.ApplicationId);
        Assert.Equal(question.QuestionId, insertedAnswer.QuestionId);
        Assert.Equal(answerJson, insertedAnswer.Answer);

        using var expectedDoc = JsonDocument.Parse(answerJson);
        using var actualDoc = JsonDocument.Parse(insertedAnswer.Answer);

        Assert.Equal(expectedDoc.RootElement.ToString(), actualDoc.RootElement.ToString());

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}