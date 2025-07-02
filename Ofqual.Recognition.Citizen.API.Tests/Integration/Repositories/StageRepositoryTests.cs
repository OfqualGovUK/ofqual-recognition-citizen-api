using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.Tests.Integration.Helper;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class StageRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public StageRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetFirstQuestionByStage_Should_Return_First_Question_In_StageOrder()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var task1 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-a",
            TaskOrderNumber = 1,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });
        var task2 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-b",
            TaskOrderNumber = 2,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var question1 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task1.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"second question\"}",
            QuestionNameUrl = "q-task1",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });
        var question2 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task2.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"first question\"}",
            QuestionNameUrl = "q-task2",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        await StageTestDataBuilder.CreateStageTask(unitOfWork, new StageTask
        {
            StageId = StageType.PreEngagement,
            TaskId = task1.TaskId,
            OrderNumber = 2,
            Enabled = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });
        await StageTestDataBuilder.CreateStageTask(unitOfWork, new StageTask
        {
            StageId = StageType.PreEngagement,
            TaskId = task2.TaskId,
            OrderNumber = 1,
            Enabled = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.StageRepository.GetFirstQuestionByStage(StageType.PreEngagement);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question2.QuestionId, result.QuestionId);
        Assert.Equal(task2.TaskId, result.TaskId);
        Assert.Equal(task2.TaskNameUrl, result.CurrentTaskNameUrl);
        Assert.Equal(question2.QuestionNameUrl, result.CurrentQuestionNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetStageQuestionByTaskAndQuestionUrl_Should_Return_Question_With_Correct_Navigation_Urls()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-name-url",
            TaskOrderNumber = 1,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork, new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var question1 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"First\"}",
            QuestionNameUrl = "question-1",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });
        var question2 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 2,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Middle\"}",
            QuestionNameUrl = "question-2",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });
        var question3 = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 3,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"label\":\"Last\"}",
            QuestionNameUrl = "question-3",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        await StageTestDataBuilder.CreateStageTask(unitOfWork, new StageTask
        {
            StageId = StageType.PreEngagement,
            TaskId = task.TaskId,
            OrderNumber = 1,
            Enabled = true,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.StageRepository.GetStageQuestionByTaskAndQuestionUrl(StageType.PreEngagement, task.TaskNameUrl, question2.QuestionNameUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(question2.QuestionId, result.QuestionId);
        Assert.Equal(task.TaskId, result.TaskId);
        Assert.Equal(task.TaskNameUrl, result.CurrentTaskNameUrl);
        Assert.Equal(question2.QuestionNameUrl, result.CurrentQuestionNameUrl);
        Assert.Equal(question1.QuestionNameUrl, result.PreviousQuestionNameUrl);
        Assert.Equal(question3.QuestionNameUrl, result.NextQuestionNameUrl);
        Assert.Equal(task.TaskNameUrl, result.PreviousTaskNameUrl);
        Assert.Equal(task.TaskNameUrl, result.NextTaskNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetStageStatus_Should_Return_Expected_StageStatusView()
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
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var stageId = StageType.PreEngagement;
        var now = DateTime.UtcNow;

        var stageStatus = await StageTestDataBuilder.CreateStageStatus(unitOfWork, new StageStatus
        {
            ApplicationId = application.ApplicationId,
            StageId = stageId,
            StatusId = StatusType.InProgress,
            StageStartDate = now,
            StageCompletionDate = now.AddDays(2),
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = now,
            ModifiedDate = now
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.StageRepository.GetStageStatus(application.ApplicationId, stageId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(application.ApplicationId, result.ApplicationId);
        Assert.Equal(stageId, result.StageId);
        Assert.Equal(StatusType.InProgress, result.StatusId);
        TestAssertHelpers.AssertDateTimeAlmostEqual(stageStatus.StageStartDate, result.StageStartDate);
        TestAssertHelpers.AssertDateTimeAlmostEqual(stageStatus.StageCompletionDate!.Value, result.StageCompletionDate!.Value);
        Assert.False(string.IsNullOrWhiteSpace(result.Status));
        Assert.False(string.IsNullOrWhiteSpace(result.StageName));

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAllStageTasksByStageId_Should_Return_All_Tasks_For_Stage()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork, new Section
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Test Section",
            SectionOrderNumber = 1,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var task1 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-a",
            TaskOrderNumber = 1,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });
        var task2 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task",
            TaskNameUrl = "task-b",
            TaskOrderNumber = 2,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var stageId = StageType.PreEngagement;
        var now = DateTime.UtcNow;

        await StageTestDataBuilder.CreateStageTask(unitOfWork, new StageTask
        {
            StageId = stageId,
            TaskId = task1.TaskId,
            OrderNumber = 1,
            Enabled = true,
            CreatedDate = now,
            ModifiedDate = now,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk"
        });
        await StageTestDataBuilder.CreateStageTask(unitOfWork, new StageTask
        {
            StageId = stageId,
            TaskId = task2.TaskId,
            OrderNumber = 2,
            Enabled = true,
            CreatedDate = now,
            ModifiedDate = now,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        // Act
        var results = (await unitOfWork.StageRepository.GetAllStageTasksByStageId(stageId))?.ToList();

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.TaskId == task1.TaskId && r.OrderNumber == 1);
        Assert.Contains(results, r => r.TaskId == task2.TaskId && r.OrderNumber == 2);
        Assert.All(results, r =>
        {
            Assert.Equal(stageId, r.StageId);
            Assert.False(string.IsNullOrWhiteSpace(r.StageName));
            Assert.False(string.IsNullOrWhiteSpace(r.Task));
        });

        // Clean up
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpsertStageStatusRecord_Should_Insert_And_Update_StageStatus()
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
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var stageId = StageType.PreEngagement;
        var now = DateTime.UtcNow;

        var initialStatus = new StageStatus
        {
            ApplicationId = application.ApplicationId,
            StageId = stageId,
            StatusId = StatusType.NotStarted,
            StageStartDate = now,
            StageCompletionDate = null,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk",
            CreatedDate = now,
            ModifiedDate = now
        };

        // Act: Insert
        var inserted = await unitOfWork.StageRepository.UpsertStageStatusRecord(initialStatus);
        unitOfWork.Commit();

        // Assert: Insert successful
        Assert.True(inserted);

        var insertedResult = await unitOfWork.StageRepository.GetStageStatus(application.ApplicationId, stageId);
        Assert.NotNull(insertedResult);
        Assert.Equal(StatusType.NotStarted, insertedResult!.StatusId);
        Assert.Null(insertedResult.StageCompletionDate);
        TestAssertHelpers.AssertDateTimeAlmostEqual(now, insertedResult.StageStartDate);

        // Act: Update existing
        var updatedStatus = new StageStatus
        {
            ApplicationId = application.ApplicationId,
            StageId = stageId,
            StatusId = StatusType.Completed,
            StageStartDate = now,
            StageCompletionDate = now.AddDays(1),
            CreatedByUpn = "ignored@ofqual.gov.uk",
            ModifiedByUpn = "updated@ofqual.gov.uk",
            CreatedDate = now,
            ModifiedDate = now
        };

        var updated = await unitOfWork.StageRepository.UpsertStageStatusRecord(updatedStatus);
        unitOfWork.Commit();

        // Assert: Update successful
        Assert.True(updated);

        var updatedResult = await unitOfWork.StageRepository.GetStageStatus(application.ApplicationId, stageId);
        Assert.NotNull(updatedResult);
        Assert.Equal(StatusType.Completed, updatedResult!.StatusId);
        Assert.Equal(now.AddDays(1).Date, updatedResult.StageCompletionDate?.Date);
        TestAssertHelpers.AssertDateTimeAlmostEqual(now, updatedResult.StageStartDate);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}