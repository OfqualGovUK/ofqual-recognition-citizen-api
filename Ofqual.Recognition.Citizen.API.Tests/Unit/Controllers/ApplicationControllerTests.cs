using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Controllers;

public class ApplicationControllerTests
{
    private readonly ApplicationController _controller;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IApplicationRepository> _mockApplicationRepository;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICheckYourAnswersService> _checkYourAnswersService;

    public ApplicationControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _checkYourAnswersService = new Mock<ICheckYourAnswersService>();

        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);

        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);

        _mockApplicationRepository = new Mock<IApplicationRepository>();
        _mockUnitOfWork.Setup(u => u.ApplicationRepository).Returns(_mockApplicationRepository.Object);

        _controller = new ApplicationController(_mockUnitOfWork.Object, _checkYourAnswersService.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
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
            var application = Assert.IsType<ApplicationDetailsDto>(okResult.Value);
            Assert.NotNull(application);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetApplicationTasks_ShouldReturnSectionsWithTasks()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var mockTaskItems = new List<TaskItemStatusSection>
        {
            new TaskItemStatusSection
            {
                SectionId = Guid.NewGuid(),
                SectionName = "Section A",
                SectionOrderNumber = 1,
                TaskId = Guid.NewGuid(),
                TaskName = "Task 1",
                TaskOrderNumber = 1,
                TaskStatusId = Guid.NewGuid(),
                Status = TaskStatusEnum.Completed,
                QuestionURL = "testurl/path"
            },
            new TaskItemStatusSection
            {
                SectionId = Guid.NewGuid(),
                SectionName = "Section A",
                SectionOrderNumber = 1,
                TaskId = Guid.NewGuid(),
                TaskName = "Task 2",
                TaskOrderNumber = 2,
                TaskStatusId = Guid.NewGuid(),
                Status = TaskStatusEnum.InProgress,
                QuestionURL = "testurl/path"
            },
            new TaskItemStatusSection
            {
                SectionId = Guid.NewGuid(),
                SectionName = "Section B",
                SectionOrderNumber = 2,
                TaskId = Guid.NewGuid(),
                TaskName = "Task 3",
                TaskOrderNumber = 1,
                TaskStatusId = Guid.NewGuid(),
                Status = TaskStatusEnum.NotStarted,
                QuestionURL = "testurl/path"
            }
        };

        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(mockTaskItems);

        var expectedSections = TaskMapper.MapToSectionsWithTasks(mockTaskItems);

        // Act
        var result = await _controller.GetApplicationTasks(applicationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSections = Assert.IsType<List<TaskItemStatusSectionDto>>(okResult.Value);

        Assert.Equal(expectedSections.Count, returnedSections.Count);
        Assert.Equal(expectedSections[0].SectionName, returnedSections[0].SectionName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetApplicationTasks_ShouldReturnBadRequest_WhenNoTasksFound()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(new List<TaskItemStatusSection>());

        // Act
        var result = await _controller.GetApplicationTasks(applicationId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No tasks found for the specified application.", badRequestResult.Value);
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

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("Answer 1", "criteria-a/next-question")]
    [InlineData("Answer 2", null)]
    public async Task SubmitQuestionAnswer_ReturnsOk_WhenAnswerSaved_AndStatusUpdated(string answer, string? nextQuestionUrl)
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = answer };

        var nextUrlDto = nextQuestionUrl != null
            ? new QuestionAnswerSubmissionResponseDto { NextQuestionUrl = nextQuestionUrl }
            : null;
        
        _mockQuestionRepository
            .Setup(r => r.InsertQuestionAnswer(applicationId, questionId, answer))
            .ReturnsAsync(true);
        
        _mockTaskRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress))
            .ReturnsAsync(true);
        
        _mockQuestionRepository
            .Setup(r => r.GetNextQuestionUrl(questionId))
            .ReturnsAsync(nextUrlDto);
        
        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        if (nextQuestionUrl == null)
        {
            Assert.Null(okResult.Value);
        }
        else
        {
            var dtoResult = Assert.IsType<QuestionAnswerSubmissionResponseDto>(okResult.Value);
            Assert.Equal(nextQuestionUrl, dtoResult.NextQuestionUrl);
        }
    }
    
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ReturnsBadRequest_WhenInsertFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Invalid Answer" };

        _mockQuestionRepository
            .Setup(r => r.InsertQuestionAnswer(applicationId, questionId, dto.Answer))
            .ReturnsAsync(false);
        
        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to save the question answer. Please check your input and try again.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ReturnsBadRequest_WhenTaskStatusUpdateFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Valid Answer" };

        _mockQuestionRepository
            .Setup(r => r.InsertQuestionAnswer(applicationId, questionId, dto.Answer))
            .ReturnsAsync(true);
        
        _mockTaskRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress))
            .ReturnsAsync(false);
        
        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to update task status. Either the task does not exist or belongs to a different application.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskQuestionAnswers_ShouldReturnReviewAnswers_WhenDataExists()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var mockAnswers = new List<TaskQuestionAnswerDto>
        {
            new TaskQuestionAnswerDto
            {
                TaskId = taskId,
                TaskName = "Task 1",
                TaskOrder = 1,
                QuestionId = Guid.NewGuid(),
                QuestionContent = "{}",
                QuestionUrl = "task/question/url",
                Answer = "{\"field\":\"value\"}"
            }
        };

        var expectedReviewAnswers = new List<QuestionAnswerReviewDto>
        {
            new QuestionAnswerReviewDto
            {
                QuestionText = "Sample Question",
                AnswerValue = "Sample Answer",
                QuestionUrl = "task/question/url"
            }
        };

        _mockQuestionRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(mockAnswers);

        _checkYourAnswersService
            .Setup(service => service.GetQuestionAnswers(mockAnswers))
            .Returns(expectedReviewAnswers);

        // Act
        var result = await _controller.GetTaskQuestionAnswers(applicationId, taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnswers = Assert.IsType<List<QuestionAnswerReviewDto>>(okResult.Value);

        Assert.Equal(expectedReviewAnswers.Count, returnedAnswers.Count);
        Assert.Equal(expectedReviewAnswers[0].AnswerValue, returnedAnswers[0].AnswerValue);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskQuestionAnswers_ShouldReturnNotFound_WhenNoAnswersExist()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _mockQuestionRepository
            .Setup(repo => repo.GetTaskQuestionAnswers(applicationId, taskId))
            .ReturnsAsync(new List<TaskQuestionAnswerDto>());

        // Act
        var result = await _controller.GetTaskQuestionAnswers(applicationId, taskId);
        
        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No question answers found for the specified task and application.", notFoundResult.Value);
    }
}