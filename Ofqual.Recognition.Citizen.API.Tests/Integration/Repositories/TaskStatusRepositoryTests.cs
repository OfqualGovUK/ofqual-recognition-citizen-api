using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;
using QuestionType = Ofqual.Recognition.Citizen.API.Core.Models.QuestionType;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class TaskStatusRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public TaskStatusRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTaskStatusesByApplicationId_Should_Return_Correct_Status_With_Metadata()
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
            HintText = "Please answer the following question",
            ReviewFlag = true,
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

        var question = await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"Question 1\"}",
            QuestionNameUrl = "question-test",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var expectedStatus = await TaskTestDataBuilder.CreateTestTaskStatus(unitOfWork, new TaskItemStatus
        {
            TaskStatusId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            TaskId = task.TaskId,
            Status = StatusType.NotStarted,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        unitOfWork.Commit();

        // Act
        var result = (await unitOfWork.TaskStatusRepository.GetTaskStatusesByApplicationId(application.ApplicationId) ?? Enumerable.Empty<TaskItemStatusSection>()).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        var status = result[0];
        Assert.Equal(section.SectionId, status.SectionId);
        Assert.Equal(section.SectionName, status.SectionName);
        Assert.Equal(task.TaskId, status.TaskId);
        Assert.Equal(task.TaskName, status.TaskName);
        Assert.Equal(expectedStatus.TaskStatusId, status.TaskStatusId);
        Assert.Equal(expectedStatus.Status, status.Status);
        Assert.Equal(question.QuestionNameUrl, status.QuestionNameUrl);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Create_Task_Statuses_For_All_Tasks()
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
            TaskName = "Test Task 1",
            TaskNameUrl = "task-name-url-1",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        var task2 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Test Task 2",
            TaskNameUrl = "task-name-url-2",
            TaskOrderNumber = 2,
            HintText = "Please answer the following question",
            ReviewFlag = true,
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

        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task1.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"task 1 question\"}",
            QuestionNameUrl = "question-test-1",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task2.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"task 2 question\"}",
            QuestionNameUrl = "question-test-2",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        var statuses = new List<TaskItemStatus>
        {
            new TaskItemStatus
            {
                TaskStatusId = Guid.NewGuid(),
                ApplicationId = application.ApplicationId,
                TaskId = task1.TaskId,
                Status = StatusType.NotStarted,
                CreatedByUpn = "test@ofqual.gov.uk",
                ModifiedByUpn = "test@ofqual.gov.uk"
            },
            new TaskItemStatus
            {
                TaskStatusId = Guid.NewGuid(),
                ApplicationId = application.ApplicationId,
                TaskId = task2.TaskId,
                Status = StatusType.NotStarted,
                CreatedByUpn = "test@ofqual.gov.uk",
                ModifiedByUpn = "test@ofqual.gov.uk"
            }
        };

        // Act
        var success = await unitOfWork.TaskStatusRepository.CreateTaskStatuses(statuses);

        // Assert
        Assert.True(success);

        var result = (await unitOfWork.TaskStatusRepository.GetTaskStatusesByApplicationId(application.ApplicationId) ?? Enumerable.Empty<TaskItemStatusSection>()).ToList();

        Assert.Equal(2, result.Count);
        foreach (var item in result)
        {
            Assert.Contains(item.TaskId, statuses.Select(s => s.TaskId));
            Assert.False(string.IsNullOrWhiteSpace(item.TaskNameUrl));
            Assert.False(string.IsNullOrWhiteSpace(item.QuestionNameUrl));
            Assert.Equal(StatusType.NotStarted, item.Status);
        }

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Update_Task_Status()
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
            HintText = "Please answer the following question",
            ReviewFlag = true,
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

        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = task.TaskId,
            QuestionOrderNumber = 1,
            QuestionTypeId = questionType.QuestionTypeId,
            QuestionContent = "{\"title\":\"Test\"}",
            QuestionNameUrl = "question-test",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        await TaskTestDataBuilder.CreateTestTaskStatus(unitOfWork, new TaskItemStatus
        {
            TaskStatusId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            TaskId = task.TaskId,
            Status = StatusType.NotStarted,
            CreatedByUpn = "test@ofqual.gov.uk",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow
        });

        unitOfWork.Commit();

        // Act
        var success = await unitOfWork.TaskStatusRepository.UpdateTaskStatus(application.ApplicationId, task.TaskId, StatusType.Completed, application.CreatedByUpn);

        // Assert
        Assert.True(success);

        var taskStatuses = (await unitOfWork.TaskStatusRepository.GetTaskStatusesByApplicationId(application.ApplicationId) ?? Enumerable.Empty<TaskItemStatusSection>()).ToList();
        Assert.Single(taskStatuses);

        var updated = taskStatuses[0];
        Assert.Equal(StatusType.Completed, updated.Status);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}