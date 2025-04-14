using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Helper;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using System.Text.Json;
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
    public async Task Should_Return_Previous_Question_Url()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var firstQuestion = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "test/first-question",
            "{\"title\":\"first question\"}"
        );

        var secondQuestion = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            2,
            "test/second-question",
            "{\"title\":\"second question\"}"
        );
        
        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetQuestion("test/second-question");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(secondQuestion.QuestionId, result!.QuestionId);
        Assert.Equal("test/first-question", result.PreviousQuestionUrl);

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

        var insertedAnswer = await QuestionTestDataBuilder.GetInsertedApplicationAnswer(
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

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Not_Allow_Duplicate_Insert_For_Same_Application_And_Question()
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
            "integration-test/duplicate-insert",
            "{\"title\":\"Duplicate check\"}"
        );

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);
        var answerJson = JsonSerializer.Serialize(new { value = "Answer once" });

        unitOfWork.Commit();

        // Act
        var firstInsert = await unitOfWork.QuestionRepository.InsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson
        );

        var secondInsert = await unitOfWork.QuestionRepository.InsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson
        );

        unitOfWork.Commit();

        // Assert
        Assert.True(firstInsert);
        Assert.False(secondInsert);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_TaskQuestionAnswers()
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
            "integration/test-url",
            "{\"label\":\"Test question content\"}"
        );

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);

        var answerJson = JsonSerializer.Serialize(new { value = "Integration answer" });

        var inserted = await unitOfWork.QuestionRepository.InsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson
        );

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.QuestionRepository.GetTaskQuestionAnswers(application.ApplicationId, task.TaskId);
        var answers = result.ToList();

        // Assert
        Assert.Single(answers);

        var returned = answers[0];
        Assert.Equal(task.TaskId, returned.TaskId);
        Assert.Equal(task.TaskName, returned.TaskName);
        Assert.Equal(task.TaskOrderNumber, returned.TaskOrder);
        Assert.Equal(question.QuestionId, returned.QuestionId);
        Assert.Equal(question.QuestionContent, returned.QuestionContent);
        Assert.Equal(question.QuestionURL, returned.QuestionUrl);
        Assert.Equal(answerJson, returned.Answer);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}