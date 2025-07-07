using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
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
    public async Task UpsertQuestionAnswer_Should_Insert_Then_Update_Answer()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-url",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionNameUrl = "question-url",
            QuestionContent = "{\"label\":\"Initial content\"}",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var initialAnswer = JsonSerializer.Serialize(new { value = "Initial answer" });
        var updatedAnswer = JsonSerializer.Serialize(new { value = "Updated answer" });

        // Act: Insert initial answer
        var insertSuccess = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            initialAnswer,
            application.CreatedByUpn
        );

        unitOfWork.Commit();

        // Assert: Validate inserted answer
        var inserted = await ApplicationAnswersTestDataBuilder.GetInsertedApplicationAnswer(
            unitOfWork,
            application.ApplicationId,
            question.QuestionId
        );

        Assert.True(insertSuccess);
        Assert.NotNull(inserted);
        Assert.Equal(initialAnswer, inserted!.Answer);

        // Act: Update with a new answer
        var updateSuccess = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            updatedAnswer,
            application.CreatedByUpn
        );

        unitOfWork.Commit();

        // Assert: Validate updated answer
        var updated = await ApplicationAnswersTestDataBuilder.GetInsertedApplicationAnswer(
            unitOfWork,
            application.ApplicationId,
            question.QuestionId
        );

        Assert.True(updateSuccess);
        Assert.NotNull(updated);
        Assert.Equal(updatedAnswer, updated!.Answer);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllApplicationAnswers_Should_Return_All_Answers_For_Application()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-url",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Test question\"}",
            QuestionNameUrl = "question-url",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var answerJson = JsonSerializer.Serialize(new { value = "My Answer" });

        var success = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson,
            application.CreatedByUpn
        );

        unitOfWork.Commit();

        // Act
        var result = (await unitOfWork.ApplicationAnswersRepository.GetAllApplicationAnswers(application.ApplicationId))?.ToList();

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Single(result);

        var item = result[0];
        Assert.Equal(application.ApplicationId, item.ApplicationId);
        Assert.Equal(question.QuestionId, item.QuestionId);
        Assert.Equal(task.TaskId, item.TaskId);
        Assert.Equal(question.QuestionContent, item.QuestionContent);
        Assert.Equal(question.QuestionNameUrl, item.QuestionNameUrl);
        Assert.Equal(task.TaskName, item.TaskName);
        Assert.Equal(task.TaskNameUrl, item.TaskNameUrl);
        Assert.Equal(task.TaskOrderNumber, item.TaskOrderNumber);
        Assert.Equal(answerJson, item.Answer);

        Assert.Equal(section.SectionId, item.SectionId);
        Assert.Equal(section.SectionName, item.SectionName);
        Assert.Equal(section.SectionOrderNumber, item.SectionOrderNumber);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTaskQuestionAnswers_Should_Return_Answers_For_Given_Task_And_Application()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Section A",
            SectionOrderNumber = 1,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-a",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "Text",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Enter something\"}",
            QuestionNameUrl = "question-a",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var answerJson = JsonSerializer.Serialize(new { value = "Test Answer" });

        var upserted = await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson,
            application.CreatedByUpn
        );

        unitOfWork.Commit();

        // Act
        var result = (await unitOfWork.ApplicationAnswersRepository
            .GetTaskQuestionAnswers(application.ApplicationId, task.TaskId))
            .ToList();

        // Assert
        Assert.True(upserted);
        Assert.Single(result);

        var answer = result[0];

        Assert.Equal(task.TaskId, answer.TaskId);
        Assert.Equal(task.TaskName, answer.TaskName);
        Assert.Equal(task.TaskNameUrl, answer.TaskNameUrl);
        Assert.Equal(task.TaskOrderNumber, answer.TaskOrderNumber);

        Assert.Equal(question.QuestionId, answer.QuestionId);
        Assert.Equal(question.QuestionNameUrl, answer.QuestionNameUrl);
        Assert.Equal(question.QuestionContent, answer.QuestionContent);

        Assert.Equal(application.ApplicationId, answer.ApplicationId);
        Assert.Equal(answerJson, answer.Answer);

        Assert.Equal(section.SectionId, answer.SectionId);
        Assert.Equal(section.SectionName, answer.SectionName);
        Assert.Equal(section.SectionOrderNumber, answer.SectionOrderNumber);

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
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-url",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "Text",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Enter data\"}",
            QuestionNameUrl = "question-1",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var answerJson = JsonSerializer.Serialize(new { value = "Some answer" });

        await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(
            application.ApplicationId,
            question.QuestionId,
            answerJson,
            application.CreatedByUpn
        );

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.ApplicationAnswersRepository.GetQuestionAnswer(
            application.ApplicationId,
            question.QuestionId
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question.QuestionId, result!.QuestionId);
        Assert.Equal(answerJson, result.Answer);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetQuestionAnswer_Should_Return_Null_When_Answer_Does_Not_Exist()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange

        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-no-answer",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "Text",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Unanswered\"}",
            QuestionNameUrl = "question-2",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.ApplicationAnswersRepository.GetQuestionAnswer(
            application.ApplicationId,
            question.QuestionId
        );

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question.QuestionId, result!.QuestionId);
        Assert.Null(result.Answer);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CheckIfQuestionAnswerExists_Should_Return_True_When_Answer_Exists()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "test-task",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"Check exists\"}",
            QuestionNameUrl = "question-exists",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var answer = JsonSerializer.Serialize(new { contact = "email" });

        await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(application.ApplicationId, question.QuestionId, answer, application.CreatedByUpn);
        unitOfWork.Commit();

        // Act
        var exists = await unitOfWork.ApplicationAnswersRepository.CheckIfQuestionAnswerExists(
            question.QuestionId,
            "contact",
            "email"
        );

        // Assert
        Assert.True(exists);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CheckIfQuestionAnswerExists_Should_Return_False_When_No_Matching_Answer()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var user = await UserTestDataBuilder.CreateTestUser(unitOfWork, new User
        {
            B2CId = Guid.NewGuid(),
            EmailAddress = "test@ofqual.gov.uk",
            DisplayName = "Ofqual Test Account",
            CreatedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedDate = DateTime.UtcNow,
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork, new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = user.UserId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Another Task",
            TaskNameUrl = "another-task",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"No match\"}",
            QuestionNameUrl = "no-match",
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        var answer = JsonSerializer.Serialize(new { contact = "phone" });

        await unitOfWork.ApplicationAnswersRepository.UpsertQuestionAnswer(application.ApplicationId, question.QuestionId, answer, application.CreatedByUpn);
        unitOfWork.Commit();

        // Act
        var exists = await unitOfWork.ApplicationAnswersRepository.CheckIfQuestionAnswerExists(
            question.QuestionId,
            "contact",
            "email"
        );

        // Assert
        Assert.False(exists);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}