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
    private readonly Mock<IApplicationAnswersRepository> _mockApplicationAnswersRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITaskStatusService> _mockTaskStatusService;
    private readonly Mock<IApplicationAnswersService> _mockApplicationAnswersService;
    private readonly Mock<IStageService> _mockStageService;
    private readonly Mock<IUserInformationService> _mockUserInformationService;
    private readonly Mock<IFeatureFlagService> _mockFeatureFlagService;

    public ApplicationControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockTaskStatusService = new Mock<ITaskStatusService>();
        _mockApplicationAnswersService = new Mock<IApplicationAnswersService>();
        _mockStageService = new Mock<IStageService>();
        _mockUserInformationService = new Mock<IUserInformationService>();
        _mockFeatureFlagService = new Mock<IFeatureFlagService>();

        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);

        _mockApplicationRepository = new Mock<IApplicationRepository>();
        _mockUnitOfWork.Setup(u => u.ApplicationRepository).Returns(_mockApplicationRepository.Object);

        _mockApplicationAnswersRepository = new Mock<IApplicationAnswersRepository>();
        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository).Returns(_mockApplicationAnswersRepository.Object);



        _controller = new ApplicationController(_mockUnitOfWork.Object, _mockTaskStatusService.Object, _mockApplicationAnswersService.Object, _mockStageService.Object, _mockUserInformationService.Object, _mockFeatureFlagService.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateApplication_ReturnsBadRequest_WhenApplicationCreationFails()
    {
        // Arrange

        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserUpn())
            .Returns(upn);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserObjectId())
            .Returns(oid);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserDisplayName())
            .Returns(displayName);

        _mockApplicationRepository.Setup(x => x.CreateApplication(oid, displayName, upn)).ReturnsAsync((Application?)null);

        // Act
        var result = await _controller.CreateApplication(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Application could not be created.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateApplication_ReturnsBadRequest_WhenTaskStatusesCreationFails()
    {
        // Arrange
        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserUpn())
            .Returns(upn);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserObjectId())
            .Returns(oid);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserDisplayName())
            .Returns(displayName);

        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "testuser@ofqual.gov.uk"
        };

        _mockApplicationRepository.Setup(x => x.CreateApplication(oid, displayName, upn)).ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, null)).ReturnsAsync(false);

        // Act
        var result = await _controller.CreateApplication(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to create task statuses for the new application.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateApplication_ReturnsBadRequest_WhenPreEngagementInsertFails()
    {
        // Arrange
        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserUpn())
            .Returns(upn);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserObjectId())
            .Returns(oid);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserDisplayName())
            .Returns(displayName);

        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "testuser@ofqual.gov.uk"
        };

        var preAnswers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto { QuestionId = Guid.NewGuid(), AnswerJson = "{}" }
        };

        _mockApplicationRepository.Setup(x => x.CreateApplication(oid, displayName, upn)).ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, preAnswers)).ReturnsAsync(true);
        _mockApplicationAnswersService.Setup(x => x.SavePreEngagementAnswers(app.ApplicationId, preAnswers)).ReturnsAsync(false);

        // Act
        var result = await _controller.CreateApplication(preAnswers);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Failed to insert pre-engagement answers for the new application.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateApplication_ReturnsBadRequest_WhenStageStatusUpdateFails()
    {
        // Arrange
        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserUpn())
            .Returns(upn);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserObjectId())
            .Returns(oid);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserDisplayName())
            .Returns(displayName);

        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "testuser@ofqual.gov.uk"
        };

        var preAnswers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto { QuestionId = Guid.NewGuid(), AnswerJson = "{}" }
        };

        _mockApplicationRepository.Setup(x => x.CreateApplication(oid, displayName, upn)).ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, preAnswers)).ReturnsAsync(true);
        _mockApplicationAnswersService.Setup(x => x.SavePreEngagementAnswers(app.ApplicationId, preAnswers)).ReturnsAsync(true);
        _mockStageService.Setup(x => x.EvaluateAndUpsertStageStatus(app.ApplicationId, Stage.PreEngagement)).ReturnsAsync(false);

        // Act
        var result = await _controller.CreateApplication(preAnswers);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Unable to determine or save the stage status for the application.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateApplication_ReturnsOk_WhenAllStepsSucceed()
    {
        // Arrange
        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserUpn())
            .Returns(upn);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserObjectId())
            .Returns(oid);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserDisplayName())
            .Returns(displayName);

        var app = new Application
        {
            ApplicationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "testuser@ofqual.gov.uk",
            ModifiedByUpn = "testuser1@ofqual.gov.uk"
        };

        var preAnswers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto { QuestionId = Guid.NewGuid(), AnswerJson = "{}" }
        };

        _mockApplicationRepository.Setup(x => x.CreateApplication(oid, displayName, upn)).ReturnsAsync(app);
        _mockTaskStatusService.Setup(x => x.DetermineAndCreateTaskStatuses(app.ApplicationId, preAnswers)).ReturnsAsync(true);
        _mockApplicationAnswersService.Setup(x => x.SavePreEngagementAnswers(app.ApplicationId, preAnswers)).ReturnsAsync(true);
        _mockStageService.Setup(x => x.EvaluateAndUpsertStageStatus(app.ApplicationId, Stage.PreEngagement)).ReturnsAsync(true);

        // Act
        var result = await _controller.CreateApplication(preAnswers);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ApplicationDetailsDto>(okResult.Value);
        Assert.Equal(app.ApplicationId, dto.ApplicationId);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateApplication_ThrowsException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        // Set up UserInformationService mock
        string upn = "test@test.com";
        string oid = Guid.NewGuid().ToString();
        string displayName = "Test Name";

        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserUpn())
            .Returns(upn);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserObjectId())
            .Returns(oid);
        _mockUserInformationService
            .Setup(_ => _.GetCurrentUserDisplayName())
            .Returns(displayName);

        _mockApplicationRepository.Setup(x => x.CreateApplication(oid, displayName, upn)).ThrowsAsync(new Exception("Database failure"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _controller.CreateApplication(null));
        Assert.Equal("An error occurred while creating the application. Please try again later.", ex.Message);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
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
                TaskNameUrl = "testurl",
                TaskOrderNumber = 1,
                TaskStatusId = Guid.NewGuid(),
                Status = TaskStatusEnum.Completed,
                QuestionNameUrl = "path"
            },
            new TaskItemStatusSection
            {
                SectionId = Guid.NewGuid(),
                SectionName = "Section A",
                SectionOrderNumber = 1,
                TaskId = Guid.NewGuid(),
                TaskName = "Task 2",
                TaskNameUrl = "testurl",
                TaskOrderNumber = 2,
                TaskStatusId = Guid.NewGuid(),
                Status = TaskStatusEnum.InProgress,
                QuestionNameUrl = "path"
            },
            new TaskItemStatusSection
            {
                SectionId = Guid.NewGuid(),
                SectionName = "Section B",
                SectionOrderNumber = 2,
                TaskId = Guid.NewGuid(),
                TaskName = "Task 3",
                TaskNameUrl = "testurl",
                TaskOrderNumber = 1,
                TaskStatusId = Guid.NewGuid(),
                Status = TaskStatusEnum.NotStarted,
                QuestionNameUrl = "path"
            }
        };

        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(mockTaskItems);

        var expectedSections = TaskMapper.ToDto(mockTaskItems);

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

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsOk_WhenUpdateSucceeds()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        _mockTaskRepository.Setup(r => r.UpdateTaskStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(true);
        _mockStageService.Setup(x => x.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

        // Assert
        Assert.IsType<OkResult>(result);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsBadRequest_WhenUpdateFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        _mockTaskRepository.Setup(r => r.UpdateTaskStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to update task status. Either the task does not exist or belongs to a different application.", badRequest.Value);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ThrowsException_WhenRepositoryThrows()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        _mockTaskRepository.Setup(r => r.UpdateTaskStatus(applicationId, taskId, request.Status))
            .ThrowsAsync(new Exception("DB error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _controller.UpdateTaskStatus(applicationId, taskId, request));
        Assert.Equal("An error occurred while updating the task status. Please try again later.", ex.Message);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskStatus_ReturnsBadRequest_WhenStageStatusUpdateFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var request = new UpdateTaskStatusDto { Status = TaskStatusEnum.Completed };

        _mockTaskRepository.Setup(r => r.UpdateTaskStatus(applicationId, taskId, request.Status))
            .ReturnsAsync(true);

        _mockStageService.Setup(x => x.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdateTaskStatus(applicationId, taskId, request);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Unable to determine or save the stage status for the application.", badRequest.Value);
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
                new ValidationErrorItem { PropertyName = "Answer", ErrorMessage = "This field is required." }
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

        _mockApplicationAnswersRepository
            .Setup(r => r.UpsertQuestionAnswer(applicationId, questionId, dto.Answer))
            .ReturnsAsync(true);

        _mockTaskRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitQuestionAnswer_ReturnsBadRequest_WhenUpsertFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Bad Answer" };

        _mockApplicationAnswersService
            .Setup(s => s.ValidateQuestionAnswers(questionId, dto.Answer))
            .ReturnsAsync(new ValidationResponse());

        _mockApplicationAnswersRepository
            .Setup(r => r.UpsertQuestionAnswer(applicationId, questionId, dto.Answer))
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
    public async Task SubmitQuestionAnswer_ReturnsBadRequest_WhenTaskStatusUpdateFails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var dto = new QuestionAnswerSubmissionDto { Answer = "Valid Answer" };

        _mockApplicationAnswersService
            .Setup(s => s.ValidateQuestionAnswers(questionId, dto.Answer))
            .ReturnsAsync(new ValidationResponse());

        _mockApplicationAnswersRepository
            .Setup(r => r.UpsertQuestionAnswer(applicationId, questionId, dto.Answer))
            .ReturnsAsync(true);

        _mockTaskRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Failed to update task status. Either the task does not exist or belongs to a different application.", badRequest.Value);
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
            .ThrowsAsync(new Exception("Unexpected failure"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _controller.SubmitQuestionAnswer(applicationId, taskId, questionId, dto));
        Assert.Equal("An error occurred while saving the answer. Please try again later.", ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskAnswerReview_ShouldReturnSectionedReviewAnswers_WhenDataExists()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var expectedReviewAnswers = new List<QuestionAnswerTaskSectionDto>
        {
            new QuestionAnswerTaskSectionDto
            {
                SectionHeading = "Test Section",
                QuestionAnswers = new List<QuestionAnswerTaskReviewDto>
                {
                    new QuestionAnswerTaskReviewDto
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
        var returnedSections = Assert.IsType<List<QuestionAnswerTaskSectionDto>>(okResult.Value);

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
            .ReturnsAsync(new List<QuestionAnswerTaskSectionDto>());

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