using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using System.Text.Json;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class ApplicationAnswersRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public ApplicationAnswersRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
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
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-url", orderNumber: 1);
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
        var success = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson
        );

        unitOfWork.Commit();

        // Assert
        Assert.True(success);

        var insertedAnswer = await ApplicationAnswersTestDataBuilder.GetInsertedApplicationAnswer(
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
    public async Task Should_Upsert_Answer_For_Same_Application_And_Question()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);
        var question = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "integration-test/upsert-test",
            "{\"title\":\"Upsert check\"}"
        );

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);
        var initialAnswer = JsonSerializer.Serialize(new { value = "First Answer" });
        var updatedAnswer = JsonSerializer.Serialize(new { value = "Updated Answer" });

        unitOfWork.Commit();

        // Act - Insert
        var insertResult = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            initialAnswer
        );

        // Verify Insert
        var insertedAnswer = await ApplicationAnswersTestDataBuilder.GetInsertedApplicationAnswer(unitOfWork, application.ApplicationId, question.QuestionId);
        Assert.NotNull(insertedAnswer);
        Assert.Equal(initialAnswer, insertedAnswer.Answer);

        // Act - Update
        var updateResult = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            updatedAnswer
        );

        unitOfWork.Commit();

        // Verify Update
        var updatedDbAnswer = await ApplicationAnswersTestDataBuilder.GetInsertedApplicationAnswer(unitOfWork, application.ApplicationId, question.QuestionId);
        Assert.NotNull(updatedDbAnswer);
        Assert.Equal(updatedAnswer, updatedDbAnswer.Answer);

        // Assert
        Assert.True(insertResult);
        Assert.True(updateResult);

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
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        var question = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "test-url",
            "{\"label\":\"Test question content\"}"
        );

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);

        var answerJson = JsonSerializer.Serialize(new { value = "Integration answer" });

        var inserted = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson
        );

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.ApplicationAnswersRepository.GetTaskQuestionAnswers(application.ApplicationId, task.TaskId);
        var answers = result.ToList();

        // Assert
        Assert.Single(answers);

        var returned = answers[0];
        Assert.Equal(task.TaskId, returned.TaskId);
        Assert.Equal(task.TaskName, returned.TaskName);
        Assert.Equal(task.TaskOrderNumber, returned.TaskOrder);
        Assert.Equal(question.QuestionId, returned.QuestionId);
        Assert.Equal(question.QuestionContent, returned.QuestionContent);
        Assert.Equal(question.QuestionNameUrl, returned.QuestionNameUrl);
        Assert.Equal(answerJson, returned.Answer);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetQuestionAnswer_Should_Return_Answer_If_Exists()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "test-task", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);
        var question = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "test-question",
            "{\"label\":\"Test question\"}"
        );

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);
        var answerJson = JsonSerializer.Serialize(new[] { "email", "phone" });
        var insertSuccess = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson
        );

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.ApplicationAnswersRepository.GetQuestionAnswer(application.ApplicationId, question.QuestionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question.QuestionId, result.QuestionId);
        Assert.Equal(answerJson, result.Answer);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetQuestionAnswer_Should_Return_Null_If_No_Answer_Exists()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-without-answer", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);
        var question = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            1,
            "question-without-answer",
            "{\"label\":\"Question with no answer\"}"
        );

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);
        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.ApplicationAnswersRepository.GetQuestionAnswer(application.ApplicationId, question.QuestionId);

        // Assert
        Assert.Null(result?.Answer);
        Assert.Equal(question.QuestionId, result?.QuestionId);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

}