using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Controllers;

public class ApplicationControllerTests
{
    private readonly ApplicationController _controller;
    private readonly Mock<ITaskRepository> _mockTaskRepository = new();
    private readonly Mock<IApplicationRepository> _mockApplicationRepository = new();
    private readonly Mock<IApplicationAnswersRepository> _mockApplicationAnswersRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ITaskStatusService> _mockTaskStatusService = new();
    private readonly Mock<IApplicationAnswersService> _mockApplicationAnswersService = new();
    private readonly Mock<IStageService> _mockStageService = new();
    private readonly Mock<IGovUkNotifyService> _mockGovUkNotifyService = new();
    private readonly Mock<IApplicationService> _mockApplicationService = new();

    public ApplicationControllerTests()
    {
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);
        _mockUnitOfWork.Setup(u => u.ApplicationRepository).Returns(_mockApplicationRepository.Object);
        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository).Returns(_mockApplicationAnswersRepository.Object);

        _controller = new ApplicationController(
            _mockUnitOfWork.Object,
            _mockTaskStatusService.Object,
            _mockApplicationAnswersService.Object,
            _mockStageService.Object,
            _mockGovUkNotifyService.Object,
            _mockApplicationService.Object
        );
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InitialiseApplication_ReturnsBadRequest_WhenApplicationCreationFails()
    {
        // Arrange
        _mockApplicationService.Setup(x => x.GetLatestApplicationForCurrentUser())
            .ReturnsAsync((ApplicationDetailsDto?)null);
        _mockApplicationService.Setup(x => x.CreateApplicationForCurrentUser())
            .ReturnsAsync((Application?)null);

        // Act
        var result = await _controller.InitialiseApplication(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Application could not be created.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InitialiseApplication_ReturnsBadRequest_WhenTaskStatusesCreationFails()
    {
        // Arrange
        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        _mockApplicationService.Setup(x => x.GetLatestApplicationForCurrentUser())
            .ReturnsAsync((ApplicationDetailsDto?)null);
        _mockApplicationService.Setup(x => x.CreateApplicationForCurrentUser())
            .ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, null))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.InitialiseApplication(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to create task statuses for the new application.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InitialiseApplication_ReturnsBadRequest_WhenPreEngagementInsertFails()
    {
        // Arrange
        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        var answers = new List<PreEngagementAnswerDto>
        {
            new() { QuestionId = Guid.NewGuid(), AnswerJson = "{}" }
        };

        _mockApplicationService.Setup(x => x.GetLatestApplicationForCurrentUser())
            .ReturnsAsync((ApplicationDetailsDto?)null);
        _mockApplicationService.Setup(x => x.CreateApplicationForCurrentUser())
            .ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, answers))
            .ReturnsAsync(true);
        _mockApplicationAnswersService.Setup(x => x.SavePreEngagementAnswers(app.ApplicationId, answers))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.InitialiseApplication(answers);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to insert pre-engagement answers for the new application.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InitialiseApplication_ReturnsBadRequest_WhenStageStatusUpdateFails()
    {
        // Arrange
        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        var answers = new List<PreEngagementAnswerDto>
        {
            new() { QuestionId = Guid.NewGuid(), AnswerJson = "{}" }
        };

        _mockApplicationService.Setup(x => x.GetLatestApplicationForCurrentUser())
            .ReturnsAsync((ApplicationDetailsDto?)null);
        _mockApplicationService.Setup(x => x.CreateApplicationForCurrentUser())
            .ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, answers))
            .ReturnsAsync(true);
        _mockApplicationAnswersService.Setup(x => x.SavePreEngagementAnswers(app.ApplicationId, answers))
            .ReturnsAsync(true);
        _mockStageService.Setup(x => x.EvaluateAndUpsertAllStageStatus(app.ApplicationId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.InitialiseApplication(answers);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Unable to determine or save the stage status for the application.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InitialiseApplication_ReturnsOk_WhenAllStepsSucceed()
    {
        // Arrange
        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "modifier@ofqual.gov.uk"
        };

        var answers = new List<PreEngagementAnswerDto>
        {
            new() { QuestionId = Guid.NewGuid(), AnswerJson = "{}" }
        };

        _mockApplicationService.Setup(x => x.GetLatestApplicationForCurrentUser())
            .ReturnsAsync((ApplicationDetailsDto?)null);
        _mockApplicationService.Setup(x => x.CreateApplicationForCurrentUser())
            .ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, answers))
            .ReturnsAsync(true);
        _mockApplicationAnswersService.Setup(x => x.SavePreEngagementAnswers(app.ApplicationId, answers))
            .ReturnsAsync(true);
        _mockStageService.Setup(x => x.EvaluateAndUpsertAllStageStatus(app.ApplicationId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.InitialiseApplication(answers);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ApplicationDetailsDto>(okResult.Value);
        Assert.Equal(app.ApplicationId, dto.ApplicationId);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task InitialiseApplication_ThrowsException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        _mockApplicationService.Setup(x => x.GetLatestApplicationForCurrentUser())
            .ReturnsAsync((ApplicationDetailsDto?)null);
        _mockApplicationService.Setup(x => x.CreateApplicationForCurrentUser())
            .ThrowsAsync(new Exception("Database failure"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _controller.InitialiseApplication(null));
        Assert.Equal("An error occurred while creating the application. Please try again later.", ex.Message);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetApplicationTasks_ShouldReturnSectionsWithTasks()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var expectedSections = new List<TaskItemStatusSectionDto>
        {
            new TaskItemStatusSectionDto
            {
                SectionName = "Section A",
                Tasks = new List<TaskItemStatusDto>
                {
                    new TaskItemStatusDto
                    {
                        TaskId = Guid.NewGuid(),
                        TaskName = "Task 1",
                        Status = TaskStatusEnum.Completed,
                        FirstQuestionUrl = "path/first-question"
                    },
                    new TaskItemStatusDto
                    {
                        TaskId = Guid.NewGuid(),
                        TaskName = "Task 2",
                        Status = TaskStatusEnum.InProgress,
                        FirstQuestionUrl = "path/second-question"
                    }
                }
            },
            new TaskItemStatusSectionDto
            {
                SectionName = "Section B",
                Tasks = new List<TaskItemStatusDto>
                {
                    new TaskItemStatusDto
                    {
                        TaskId = Guid.NewGuid(),
                        TaskName = "Task 3",
                        Status = TaskStatusEnum.NotStarted,
                        FirstQuestionUrl = "path/third-question"
                    }
                }
            }
        };

        _mockTaskStatusService
            .Setup(s => s.GetTaskStatusesForApplication(applicationId))
            .ReturnsAsync(expectedSections);

        // Act
        var result = await _controller.GetApplicationTasks(applicationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSections = Assert.IsType<List<TaskItemStatusSectionDto>>(okResult.Value);

        Assert.Equal(expectedSections.Count, returnedSections.Count);
        Assert.Equal(expectedSections[0].SectionName, returnedSections[0].SectionName);
        Assert.Equal(expectedSections[0].Tasks.Count(), returnedSections[0].Tasks.Count());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetApplicationTasks_ShouldReturnNotFound_WhenNoTasksFound()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        _mockTaskStatusService
            .Setup(s => s.GetTaskStatusesForApplication(applicationId))
            .ReturnsAsync((List<TaskItemStatusSectionDto>?)null);

        // Act
        var result = await _controller.GetApplicationTasks(applicationId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No tasks found for the specified application.", notFoundResult.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetApplicationTasks_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        _mockTaskStatusService
            .Setup(s => s.GetTaskStatusesForApplication(applicationId))
            .ThrowsAsync(new Exception("Unexpected failure"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _controller.GetApplicationTasks(applicationId));

        Assert.Equal("An error occurred while fetching tasks for the application. Please try again later.", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsOkWithApplication_WhenSubmissionIsTrue()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        var applicationDto = new ApplicationDetailsDto
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            Submitted = true
        };

        _mockTaskStatusService
            .Setup(s => s.UpdateTaskAndStageStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(true);

        _mockApplicationService
            .Setup(s => s.CheckAndSubmitApplication(applicationId))
            .ReturnsAsync(applicationDto);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ApplicationDetailsDto>(okResult.Value);
        Assert.Equal(applicationDto.ApplicationId, returnedDto.ApplicationId);
        Assert.True(returnedDto.Submitted);

        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsOkWithApplication_WhenSubmissionIsFalse()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        var applicationDto = new ApplicationDetailsDto
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            Submitted = false
        };

        _mockTaskStatusService
            .Setup(s => s.UpdateTaskAndStageStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(true);

        _mockApplicationService
            .Setup(s => s.CheckAndSubmitApplication(applicationId))
            .ReturnsAsync(applicationDto);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedDto = Assert.IsType<ApplicationDetailsDto>(okResult.Value);
        Assert.Equal(applicationDto.ApplicationId, returnedDto.ApplicationId);
        Assert.False(returnedDto.Submitted);

        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsBadRequest_WhenRequestIsNull()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, null!);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Request body cannot be null.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsBadRequest_WhenTaskUpdateFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        _mockTaskStatusService
            .Setup(s => s.UpdateTaskAndStageStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Unable to update task or stage status. Please try again.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsBadRequest_WhenApplicationServiceReturnsNull()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        _mockTaskStatusService
            .Setup(s => s.UpdateTaskAndStageStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(true);

        _mockApplicationService
            .Setup(s => s.CheckAndSubmitApplication(applicationId))
            .ReturnsAsync((ApplicationDetailsDto?)null);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Unable to determine application submission status.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ThrowsException_WhenServiceThrows()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.InProgress };

        _mockTaskStatusService
            .Setup(s => s.UpdateTaskAndStageStatus(applicationId, taskId, request.Status))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _controller.UpdateTaskStatus(applicationId, taskId, request));

        Assert.Equal("An error occurred while updating the task status. Please try again later.", ex.Message);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ReturnsBadRequest_WhenValidationErrorsExist()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "invalid" };

        var validationResponse = new ValidationResponse
        {
            Errors = new List<ValidationErrorItem>
            {
                new() { PropertyName = "Answer", ErrorMessage = "This field is required." }
            }
        };

        _mockApplicationAnswersService
            .Setup(s => s.ValidateQuestionAnswers(questionId, dto.Answer))
            .ReturnsAsync(validationResponse);

        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(validationResponse, badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ReturnsNoContent_WhenValidAndSaveSucceeds()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Answer A" };

        _mockApplicationAnswersService
            .Setup(s => s.ValidateQuestionAnswers(questionId, dto.Answer))
            .ReturnsAsync(new ValidationResponse());

        _mockApplicationAnswersService
            .Setup(s => s.SubmitAnswerAndUpdateStatus(applicationId, taskId, questionId, dto.Answer))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ReturnsBadRequest_WhenValidationResponseIsNull()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Test" };

        _mockApplicationAnswersService
            .Setup(s => s.ValidateQuestionAnswers(questionId, dto.Answer))
            .ReturnsAsync((ValidationResponse?)null);

        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("We could not check your answer. Please try again.", badRequest.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ReturnsBadRequest_WhenSubmitFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Answer" };

        _mockApplicationAnswersService
            .Setup(s => s.ValidateQuestionAnswers(questionId, dto.Answer))
            .ReturnsAsync(new ValidationResponse());

        _mockApplicationAnswersService
            .Setup(s => s.SubmitAnswerAndUpdateStatus(applicationId, taskId, questionId, dto.Answer))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to save the question answer. Please check your input and try again.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ThrowsException_WhenUnhandledErrorOccurs()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Any" };

        _mockApplicationAnswersService
            .Setup(s => s.ValidateQuestionAnswers(questionId, dto.Answer))
            .ThrowsAsync(new Exception("Something went wrong"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto));

        Assert.Equal("An error occurred while saving the answer. Please try again later.", ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskAnswerReview_ShouldReturnSectionedReviewAnswers_WhenDataExists()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var expectedReviewAnswers = new List<TaskReviewGroupDto>
        {
            new TaskReviewGroupDto
            {
                SectionHeading = "Test Section",
                QuestionAnswers = new List<TaskReviewItemDto>
                {
                    new TaskReviewItemDto
                    {
                        QuestionText = "Sample Question",
                        AnswerValue = new List<string> { "Sample Answer" },
                        QuestionUrl = "task-url/question-url"
                    }
                }
            }
        };
        _mockApplicationAnswersService
            .Setup(service => service.GetTaskAnswerReview(applicationId, taskId))
            .ReturnsAsync(expectedReviewAnswers);

        // Act
        var result = await _controller.GetTaskAnswerReview(applicationId, taskId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSections = Assert.IsType<List<TaskReviewGroupDto>>(okResult.Value);

        Assert.Single(returnedSections);
        var section = returnedSections[0];

        Assert.Equal("Test Section", section.SectionHeading);
        Assert.Single(section.QuestionAnswers);
        Assert.Equal("Sample Question", section.QuestionAnswers[0].QuestionText);
        Assert.Equal("Sample Answer", section.QuestionAnswers[0].AnswerValue![0]);
        Assert.Equal("task-url/question-url", section.QuestionAnswers[0].QuestionUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskAnswerReview_ShouldReturnNotFound_WhenNoAnswersExist()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _mockApplicationAnswersService
            .Setup(service => service.GetTaskAnswerReview(applicationId, taskId))
            .ReturnsAsync(new List<TaskReviewGroupDto>());

        // Act
        var result = await _controller.GetTaskAnswerReview(applicationId, taskId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No question answers found for the specified task and application.", notFoundResult.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetQuestionAnswer_ReturnsOk_WhenAnswerExists()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var expectedAnswer = new QuestionAnswerDto
        {
            QuestionId = questionId,
            Answer = "Sample Answer"
        };

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetQuestionAnswer(applicationId, questionId))
            .ReturnsAsync(expectedAnswer);

        // Act
        var result = await _controller.GetQuestionAnswer(applicationId, questionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedAnswer = Assert.IsType<QuestionAnswerDto>(okResult.Value);
        Assert.Equal(expectedAnswer.QuestionId, returnedAnswer.QuestionId);
        Assert.Equal(expectedAnswer.Answer, returnedAnswer.Answer);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetQuestionAnswer_ReturnsNotFound_WhenAnswerIsNull()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        _mockApplicationAnswersRepository
            .Setup(repo => repo.GetQuestionAnswer(applicationId, questionId))
            .ReturnsAsync((QuestionAnswerDto?)null);

        // Act
        var result = await _controller.GetQuestionAnswer(applicationId, questionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No answer found for the specified question and application.", notFoundResult.Value);
    }
}