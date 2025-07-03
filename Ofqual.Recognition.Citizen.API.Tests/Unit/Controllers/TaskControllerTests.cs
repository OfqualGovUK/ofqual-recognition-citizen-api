using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Controllers;

public class TaskControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ITaskService> _mockTaskService = new();
    private readonly TaskController _controller;

    public TaskControllerTests()
    {
        _controller = new TaskController(_mockUnitOfWork.Object, _mockTaskService.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskByTaskNameUrl_ReturnsOkResult_WhenTaskExists()
    {
        // Arrange
        var taskNameUrl = "sample-task";
        var dto = new TaskItemDto
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Sample Task",
            TaskNameUrl = taskNameUrl,
            TaskOrderNumber = 1,
            SectionId = Guid.NewGuid(),
            Stage = StageType.MainApplication
        };

        _mockTaskService
            .Setup(s => s.GetTaskWithStatusByUrl(taskNameUrl))
            .ReturnsAsync(dto);

        // Act
        var actionResult = await _controller.GetTaskByTaskNameUrl(taskNameUrl);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var returnedDto = Assert.IsType<TaskItemDto>(okResult.Value);
        Assert.Equal(dto.TaskId, returnedDto.TaskId);
        Assert.Equal(dto.TaskName, returnedDto.TaskName);
        Assert.Equal(dto.TaskNameUrl, returnedDto.TaskNameUrl);
        Assert.Equal(dto.Stage, returnedDto.Stage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskByTaskNameUrl_ReturnsBadRequest_WhenTaskDoesNotExist()
    {
        // Arrange
        var taskNameUrl = "non-existent-task";

        _mockTaskService
            .Setup(s => s.GetTaskWithStatusByUrl(taskNameUrl))
            .ReturnsAsync((TaskItemDto?)null);

        // Act
        var actionResult = await _controller.GetTaskByTaskNameUrl(taskNameUrl);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.Equal($"No task found with URL: {taskNameUrl}", badRequestResult.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskByTaskNameUrl_ThrowsException_WhenServiceThrows()
    {
        // Arrange
        var taskNameUrl = "error-task";

        _mockTaskService
            .Setup(s => s.GetTaskWithStatusByUrl(taskNameUrl))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(async () =>
            await _controller.GetTaskByTaskNameUrl(taskNameUrl));
        Assert.Equal("An error occurred while fetching the task. Please try again later.", ex.Message);
    }
}