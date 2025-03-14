using Xunit;
using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Controllers;

public class RecognitionCitizenControllerTests
{
    private readonly RecognitionCitizenController _controller;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IApplicationRepository> _mockApplicationRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public RecognitionCitizenControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);

        _mockApplicationRepository = new Mock<IApplicationRepository>();
        _mockUnitOfWork.Setup(u => u.ApplicationRepository).Returns(_mockApplicationRepository.Object);

        _controller = new RecognitionCitizenController(_mockUnitOfWork.Object);

        _controller.ControllerContext = new ControllerContext();
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateApplication_ShouldReturnExpectedResult(bool isSuccess)
    {
        // Arrange
        var mockApplication = isSuccess
            ? new Application
            {
                ApplicationId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedByUpn = "testuser@domain.com",
                ModifiedByUpn = "testuser@domain.com",
                ModifiedDate = DateTime.UtcNow
            }
            : null;

        _mockApplicationRepository.Setup(repo => repo.CreateApplication())
            .ReturnsAsync(mockApplication);

        // Act
        var result = await _controller.CreateApplication();
        
        // Assert
        if (isSuccess)
        {
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var application = Assert.IsType<Application>(okResult.Value);
            Assert.NotNull(application);
        }
        else
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Application could not be created.", badRequestResult.Value);
        }
    }

    [Fact]
    public async Task GetApplicationTasks_ShouldReturnTasks()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var mockTasks = new List<TaskStatusDto>
        {
            new TaskStatusDto { TaskId = Guid.NewGuid(), TaskName = "Test1", SectionId = Guid.NewGuid(), OrderNumber = 1, Status = TaskStatusEnum.Completed },
            new TaskStatusDto { TaskId = Guid.NewGuid(), TaskName = "Test2", SectionId = Guid.NewGuid(), OrderNumber = 2, Status = TaskStatusEnum.InProgress }
        };

        _mockTaskRepository.Setup(repo => repo.GetTasksByApplicationId(applicationId))
            .ReturnsAsync(mockTasks);

        // Act
        var result = await _controller.GetApplicationTasks(applicationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedTasks = Assert.IsType<List<TaskStatusDto>>(okResult.Value);
        Assert.Equal(mockTasks.Count, returnedTasks.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateTaskStatus_ShouldReturnExpectedResult(bool isSuccess)
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var status = TaskStatusEnum.Completed;
        
        _mockTaskRepository.Setup(repo => repo.UpdateTaskStatus(applicationId, taskId, status))
            .ReturnsAsync(isSuccess);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, status);

        // Assert
        if (isSuccess)
        {
            Assert.IsType<OkResult>(result);
        }
        else
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Failed to update task status. Either the task does not exist or belongs to a different application.", badRequestResult.Value);
        }
    }
}