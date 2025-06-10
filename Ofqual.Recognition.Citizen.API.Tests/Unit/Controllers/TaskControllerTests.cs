using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Controllers;

public class TaskControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly TaskController _controller;

    public TaskControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);
        
        _controller = new TaskController(_mockUnitOfWork.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskByTaskNameUrl_ReturnsOkResult_WhenTaskExists()
    {
        // Arrange
        var taskNameUrl = "sample-task";
        var taskItem = new TaskItem
        {
            TaskId = Guid.NewGuid(),
            TaskName = "Sample Task",
            TaskNameUrl = taskNameUrl,
            TaskOrderNumber = 1,
            SectionId = Guid.NewGuid(),
            CreatedByUpn = "test"
        };

        _mockTaskRepository.Setup(r => r.GetTaskByTaskNameUrl(taskNameUrl))
                           .ReturnsAsync(taskItem);
        // Act
        var result = await _controller.GetTaskByTaskNameUrl(taskNameUrl);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<TaskItemDto>(okResult.Value);
        Assert.Equal(taskItem.TaskName, dto.TaskName);
        Assert.Equal(taskItem.TaskNameUrl, dto.TaskNameUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskByTaskNameUrl_ReturnsBadRequest_WhenTaskDoesNotExist()
    {
        // Arrange
        var taskNameUrl = "non-existent-task";

        _mockTaskRepository.Setup(r => r.GetTaskByTaskNameUrl(taskNameUrl))
                           .ReturnsAsync((TaskItem?)null);

        // Act
        var result = await _controller.GetTaskByTaskNameUrl(taskNameUrl);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal($"No task found with URL: {taskNameUrl}", badRequestResult.Value);
    }
}
