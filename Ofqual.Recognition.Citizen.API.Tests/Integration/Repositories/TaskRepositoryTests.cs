using Ofqual.Recognition.Citizen.Tests.Integration.Fixtures;
using Ofqual.Recognition.Citizen.Tests.Integration.Builders;
using Ofqual.Recognition.Citizen.API.Core.Models;
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
    public async Task GetAllTask_Should_Return_Inserted_Task()
    {
        // Initialise test container and connection
        var unitOfWork = await _fixture.InitNewTestDatabaseContainer();

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

        var expectedTask = await TaskTestDataBuilder.CreateTestTask(unitOfWork, new TaskItem
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

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.TaskRepository.GetAllTask();
        var tasks = result.Cast<TaskItem>().ToList();
        var actualTask = tasks.FirstOrDefault(t => t.TaskId == expectedTask.TaskId);

        // Assert
        Assert.NotNull(tasks);
        Assert.NotEmpty(tasks);
        Assert.NotNull(actualTask);
        Assert.Equal(expectedTask.TaskName, actualTask!.TaskName);
        Assert.Equal(expectedTask.SectionId, actualTask.SectionId);

        // Clean up test container
        await _fixture.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Should_Return_Task_By_TaskNameUrl()
    {
        // Initialise test container and connection
        var unitOfWork = await _fixture.InitNewTestDatabaseContainer();

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
            TaskNameUrl = "unique-task-url",
            TaskOrderNumber = 1,
            HintText = "Please answer the following question",
            ReviewFlag = true,
            SectionId = section.SectionId,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        });

        unitOfWork.Commit();

        // Act
        var result = await unitOfWork.TaskRepository.GetTaskByTaskNameUrl(task.TaskNameUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(task.TaskId, result!.TaskId);
        Assert.Equal(task.TaskName, result.TaskName);
        Assert.Equal(task.TaskNameUrl, result.TaskNameUrl);
        Assert.Equal(task.SectionId, result.SectionId);
        
        // Clean up test container
        await _fixture.DisposeAsync();
    }
}