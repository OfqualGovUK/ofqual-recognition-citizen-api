using Xunit;
using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

namespace Ofqual.Recognition.Citizen.Tests.Controllers;

public class ApplicationControllerTests
{
    private readonly ApplicationController _controller;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IApplicationRepository> _mockApplicationRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITaskService> _mockTaskService;

    public ApplicationControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTaskService = new Mock<ITaskService>();

        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);

        _mockApplicationRepository = new Mock<IApplicationRepository>();
        _mockUnitOfWork.Setup(u => u.ApplicationRepository).Returns(_mockApplicationRepository.Object);

        _controller = new ApplicationController(_mockUnitOfWork.Object, _mockTaskService.Object);

        _controller.ControllerContext = new ControllerContext();
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(true, true, true)]  // Application and task statuses created successfully
    [InlineData(true, true, false)] // Task statuses failed
    [InlineData(true, false, false)] // No tasks found
    [InlineData(false, false, false)] // Application creation failed
    public async Task CreateApplication_ShouldReturnExpectedResult(bool isApplicationCreated, bool areTasksFound, bool isTaskStatusesCreated)
    {
        // Arrange
        var createdByUpn = "testuser@ofqual.com";
        var modifiedByUpn = "testuser1@ofqual.com";

        var mockApplication = isApplicationCreated
            ? new Application
            {
                ApplicationId = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                CreatedByUpn = createdByUpn,
                ModifiedByUpn = modifiedByUpn,
                ModifiedDate = DateTime.UtcNow
            }
        : null;

        var mockTasks = areTasksFound
            ? new List<TaskItem>
            {
            new TaskItem { TaskId = Guid.NewGuid(), TaskName = "Task 1", SectionId = Guid.NewGuid(), TaskOrderNumber = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedByUpn = createdByUpn, ModifiedByUpn = modifiedByUpn },
            new TaskItem { TaskId = Guid.NewGuid(), TaskName = "Task 2", SectionId = Guid.NewGuid(), TaskOrderNumber = 2, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, CreatedByUpn = createdByUpn, ModifiedByUpn = modifiedByUpn }
            }
            : new List<TaskItem>();

        _mockApplicationRepository.Setup(repo => repo.CreateApplication())
            .ReturnsAsync(mockApplication);

        _mockTaskRepository.Setup(repo => repo.GetAllTask())
            .ReturnsAsync(mockTasks);

        _mockTaskRepository.Setup(repo => repo.CreateTaskStatuses(It.IsAny<Guid>(), It.IsAny<List<TaskItem>>()))
            .ReturnsAsync(isTaskStatusesCreated);

        // Act
        var result = await _controller.CreateApplication();

        // Assert
        if (!isApplicationCreated)
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Application could not be created.", badRequestResult.Value);
        }
        else if (!areTasksFound)
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("No tasks found to create statuses for the application.", badRequestResult.Value);
        }
        else if (!isTaskStatusesCreated)
        {
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Failed to create task statuses for the new application.", badRequestResult.Value);
        }
        else
        {
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var application = Assert.IsType<ApplicationDto>(okResult.Value);
            Assert.NotNull(application);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetApplicationTasks_ShouldReturnSectionsWithTasks()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var mockSections = new List<TaskItemStatusSectionDto>
        {
            new TaskItemStatusSectionDto
            {
                SectionName = "Section A",
                Tasks = new List<TaskItemStatusDto>
                {
                    new TaskItemStatusDto { TaskId = Guid.NewGuid(), TaskName = "Task 1", Status = TaskStatusEnum.Completed, FirstQuestionUrl = "testurl/path" },
                    new TaskItemStatusDto { TaskId = Guid.NewGuid(), TaskName = "Task 2", Status = TaskStatusEnum.InProgress, FirstQuestionUrl = "testurl/path" }
                }
            },
            new TaskItemStatusSectionDto
            {
                SectionName = "Section B",
                Tasks = new List<TaskItemStatusDto>
                {
                    new TaskItemStatusDto { TaskId = Guid.NewGuid(), TaskName = "Task 3", Status = TaskStatusEnum.NotStarted, FirstQuestionUrl = "testurl/path" }
                }
            }
        };

        _mockTaskService.Setup(service => service.GetSectionsWithTasksByApplicationId(applicationId))
            .ReturnsAsync(mockSections);

        // Act
        var result = await _controller.GetApplicationTasks(applicationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSections = Assert.IsType<List<TaskItemStatusSectionDto>>(okResult.Value);

        // Validate the count of sections and tasks
        Assert.Equal(mockSections.Count, returnedSections.Count);
        Assert.Equal(mockSections[0].Tasks.Count(), returnedSections[0].Tasks.Count());
        Assert.Equal(mockSections[1].Tasks.Count(), returnedSections[1].Tasks.Count());
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateTaskStatus_ShouldReturnExpectedResult(bool isSuccess)
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var request = new UpdateTaskStatusDto
        {
            Status = TaskStatusEnum.Completed
        };

        _mockTaskRepository.Setup(repo => repo.UpdateTaskStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(isSuccess);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

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