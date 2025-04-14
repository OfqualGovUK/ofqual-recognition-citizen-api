using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Helper;
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
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);

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
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
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
        Assert.Equal(question.QuestionURL, item.QuestionURL);

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
        var task1 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
        var task2 = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
        var questionType = await QuestionTestDataBuilder.CreateTestQuestionType(unitOfWork);

        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task1.TaskId, questionType.QuestionTypeId, 1, "url-task-1", "{\"title\":\"task 1 question\"}");
        await QuestionTestDataBuilder.CreateTestQuestion(unitOfWork, task2.TaskId, questionType.QuestionTypeId, 1, "url-task-2", "{\"title\":\"task 2 question\"}");

        unitOfWork.Commit();

        var tasks = new List<TaskItem> { task1, task2 };

        // Act
        var success = await unitOfWork.TaskRepository.CreateTaskStatuses(application.ApplicationId, tasks);

        // Assert
        Assert.True(success);

        // Verify
        var result = (await unitOfWork.TaskRepository.GetTaskStatusesByApplicationId(application.ApplicationId)).ToList();
        Assert.Equal(2, result.Count);

        foreach (var taskStatus in result)
        {
            Assert.Contains(taskStatus.TaskId, tasks.Select(t => t.TaskId));
            Assert.Equal(TaskStatusEnum.NotStarted, taskStatus.Status);
            Assert.False(string.IsNullOrWhiteSpace(taskStatus.QuestionURL));
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
        var task = await TaskTestDataBuilder.CreateTestTask(unitOfWork, section.SectionId);
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
}