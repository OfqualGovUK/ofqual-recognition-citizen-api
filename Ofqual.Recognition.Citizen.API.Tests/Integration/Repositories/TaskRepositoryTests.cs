using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Repositories;

public class TaskRepositoryTests : IClassFixture<SqlTestFixture>
{
    private readonly SqlTestFixture _fixture;

    public TaskRepositoryTests(SqlTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_All_Tasks()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-url", orderNumber: 1);

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.TaskRepository.GetAllTask();

        var taskList = result.Cast<TaskItem>().ToList();

        // Assert
        Assert.NotNull(taskList);
        Assert.True(taskList.Any());

        var fetchedTask = taskList.FirstOrDefault(t => t.TaskId == task.TaskId);

        Assert.NotNull(fetchedTask);
        Assert.Equal(task.TaskName, fetchedTask!.TaskName);
        Assert.Equal(task.SectionId, fetchedTask.SectionId);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Task_Statuses_By_ApplicationId()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);
        var taskStatus = await TaskTestDataBuilder.CreateTestTaskStatus(unitOfWork, application.ApplicationId, task);
        var question = await QuestionTestDataBuilder.CreateTestQuestion(
            unitOfWork,
            task.TaskId,
            questionType.QuestionTypeId,
            order: 1,
            url: "test/test-url",
            content: "{\"title\":\"Question 1\"}"
        );

        unitOfWork.Commit();

        // Act
        var result = (await unitOfWork.TaskRepository.GetTaskStatusesByApplicationId(application.ApplicationId)).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);

        var item = result[0];
        Assert.Equal(section.SectionId, item.SectionId);
        Assert.Equal(section.SectionName, item.SectionName);
        Assert.Equal(task.TaskId, item.TaskId);
        Assert.Equal(task.TaskName, item.TaskName);
        Assert.Equal(taskStatus.TaskStatusId, item.TaskStatusId);
        Assert.Equal((int)taskStatus.Status, (int)item.Status);
        Assert.Equal(question.QuestionNameUrl, item.QuestionNameUrl);

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
        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task1 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-1-url", orderNumber: 1);
        var task2 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-2-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task1.TaskId, questionType.QuestionTypeId, 1, "url-task-1", "{\"title\":\"task 1 question\"}");
        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task2.TaskId, questionType.QuestionTypeId, 1, "url-task-2", "{\"title\":\"task 2 question\"}");

        unitOfWork.Commit();

        var statuses = new List<TaskItemStatus>
        {
            new TaskItemStatus
            {
                TaskStatusId = Guid.NewGuid(),
                ApplicationId = application.ApplicationId,
                TaskId = task1.TaskId,
                Status = TaskStatusEnum.NotStarted,
                CreatedByUpn = "test@ofqual.gov.uk",
                ModifiedByUpn = "test@ofqual.gov.uk"
            },
            new TaskItemStatus
            {
                TaskStatusId = Guid.NewGuid(),
                ApplicationId = application.ApplicationId,
                TaskId = task2.TaskId,
                Status = TaskStatusEnum.NotStarted,
                CreatedByUpn = "test@ofqual.gov.uk",
                ModifiedByUpn = "test@ofqual.gov.uk"
            }
        };

        // Act
        var success = await unitOfWork.TaskRepository.CreateTaskStatuses(statuses);

        // Assert
        Assert.True(success);

        // Act
        var result = (await unitOfWork.TaskRepository.GetTaskStatusesByApplicationId(application.ApplicationId)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        foreach (var item in result)
        {
            Assert.Contains(item.TaskId, statuses.Select(s => s.TaskId));
            Assert.Contains(item.TaskName, new[] { "Test Task" });
            Assert.False(string.IsNullOrWhiteSpace(item.TaskNameUrl));
            Assert.False(string.IsNullOrWhiteSpace(item.QuestionNameUrl));
            Assert.Equal(TaskStatusEnum.NotStarted, item.Status);
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
        var application = await ApplicationTestDataBuilder.CreateTestApplication(unitOfWork);
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "task-name-url", orderNumber: 1);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task.TaskId, questionType.QuestionTypeId, 1, "test-url", "{\"title\":\"Test\"}");

        // Insert initial TaskStatus
        await TaskTestDataBuilder.CreateTestTaskStatus(unitOfWork, application.ApplicationId, task);
        unitOfWork.Commit();

        // Act
        var success = await unitOfWork.TaskRepository.UpdateTaskStatus(application.ApplicationId, task.TaskId, TaskStatusEnum.Completed);

        // Assert
        Assert.True(success);

        var taskStatuses = (await unitOfWork.TaskRepository.GetTaskStatusesByApplicationId(application.ApplicationId)).ToList();
        Assert.Single(taskStatuses);

        var updated = taskStatuses[0];
        Assert.Equal(TaskStatusEnum.Completed, updated.Status);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Task_By_TaskNameUrl()
    {
        // Initialise test container and connection
        await using var connection = await _fixture.InitNewTestDatabaseContainer();
        using var unitOfWork = new UnitOfWork(connection);

        // Arrange
        var section = await TaskTestDataBuilder.CreateTestSection(unitOfWork);
        var expectedTask = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId, "unique-task-url", orderNumber: 1);
        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.TaskRepository.GetTaskByTaskNameUrl("unique-task-url");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTask.TaskId, result!.TaskId);
        Assert.Equal(expectedTask.TaskName, result.TaskName);
        Assert.Equal(expectedTask.TaskNameUrl, result.TaskNameUrl);
        Assert.Equal(expectedTask.SectionId, result.SectionId);

        // Clean up test container
        await _fixture.DisposeAsync();
    }
}