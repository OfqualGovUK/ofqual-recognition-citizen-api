using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class TaskStatusServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ITaskRepository> _mockTaskRepository = new();
    private readonly Mock<ITaskStatusRepository> _mockTaskStatusRepository = new();
    private readonly Mock<IApplicationAnswersRepository> _mockApplicationAnswersRepository = new();
    private readonly Mock<IApplicationRepository> _mockApplicationRepository = new();
    private readonly Mock<IStageRepository> _mockStageRepository = new();
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();
    private readonly Mock<IStageService> _mockStageService = new();
    private readonly TaskStatusService _service;

    public TaskStatusServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);
        _mockUnitOfWork.Setup(u => u.TaskStatusRepository).Returns(_mockTaskStatusRepository.Object);
        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository).Returns(_mockApplicationAnswersRepository.Object);
        _mockUnitOfWork.Setup(u => u.StageRepository).Returns(_mockStageRepository.Object);
        _mockUnitOfWork.Setup(u => u.ApplicationRepository).Returns(_mockApplicationRepository.Object);

        _service = new TaskStatusService(
            _mockUnitOfWork.Object,
            _mockUserInformationService.Object,
            _mockStageService.Object
        );
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskAndStageStatus_Should_Return_True_When_Task_And_AllStages_Are_Updated()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(s => s.GetCurrentUserUpn())
            .Returns(upn);

        _mockTaskStatusRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, StatusType.Completed, upn))
            .ReturnsAsync(true);

        _mockStageService
            .Setup(s => s.EvaluateAndUpsertAllStageStatus(applicationId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, StatusType.Completed);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskAndStageStatus_Should_Return_True_When_Task_Updated_And_Status_Not_Completed()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(s => s.GetCurrentUserUpn())
            .Returns(upn);

        _mockTaskStatusRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, StatusType.InProgress, upn))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, StatusType.InProgress);

        // Assert
        Assert.True(result);
        _mockStageService.Verify(s => s.EvaluateAndUpsertAllStageStatus(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskAndStageStatus_Should_Return_False_When_Task_Update_Fails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(s => s.GetCurrentUserUpn())
            .Returns(upn);

        _mockTaskStatusRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, StatusType.Completed, upn))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, StatusType.Completed);

        // Assert
        Assert.False(result);
        _mockStageService.Verify(s => s.EvaluateAndUpsertAllStageStatus(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskAndStageStatus_Should_Return_False_When_AllStageStatus_Update_Fails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService
            .Setup(s => s.GetCurrentUserUpn())
            .Returns(upn);

        _mockTaskStatusRepository
            .Setup(r => r.UpdateTaskStatus(applicationId, taskId, StatusType.Completed, upn))
            .ReturnsAsync(true);

        _mockStageService
            .Setup(s => s.EvaluateAndUpsertAllStageStatus(applicationId))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, StatusType.Completed);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskStatusesForApplication_ShouldReturnTasksWithCorrectHint_WhenNotSubmitted()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var declarationTaskId = Guid.NewGuid();

        var taskStatuses = new List<TaskItemStatusSection>
    {
        new TaskItemStatusSection
        {
            SectionId = Guid.NewGuid(),
            SectionName = "Section A",
            SectionOrderNumber = 1,
            TaskId = declarationTaskId,
            TaskName = "Declaration Task",
            TaskNameUrl = "declaration-task",
            TaskOrderNumber = 1,
            TaskStatusId = Guid.NewGuid(),
            Status = StatusType.CannotStartYet,
            QuestionNameUrl = "q1"
        }
    };

        var application = new Application
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            SubmittedDate = null,
            ApplicationReleaseDate = DateTime.UtcNow.AddDays(-1),
            OrganisationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk"
        };

        _mockTaskStatusRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(taskStatuses);

        _mockApplicationRepository
            .Setup(r => r.GetApplicationById(applicationId))
            .ReturnsAsync(application);

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(StageType.Declaration))
            .ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = declarationTaskId } });

        // Act
        var result = await _service.GetTaskStatusesForApplication(applicationId);

        // Assert
        Assert.NotNull(result);
        var dtoList = result.ToList();
        Assert.Contains(dtoList.SelectMany(s => s.Tasks),
            t => t.TaskId == declarationTaskId && t.HintText == "You must complete all sections first");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskStatusesForApplication_ShouldReturnNotYetReleasedHint_IfBothStagesCompleted()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var declarationTaskId = Guid.NewGuid();

        var taskStatuses = new List<TaskItemStatusSection>
        {
            new TaskItemStatusSection
            {
                SectionId = Guid.NewGuid(),
                SectionName = "Section A",
                SectionOrderNumber = 1,
                TaskId = declarationTaskId,
                TaskName = "Declaration Task",
                TaskNameUrl = "declaration-task",
                TaskOrderNumber = 1,
                TaskStatusId = Guid.NewGuid(),
                Status = StatusType.CannotStartYet,
                QuestionNameUrl = "q1"
            }
        };

        var application = new Application
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            ApplicationReleaseDate = DateTime.UtcNow.AddDays(2),
            OrganisationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk"
        };

        _mockTaskStatusRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(taskStatuses);

        _mockApplicationRepository
            .Setup(r => r.GetApplicationById(applicationId))
            .ReturnsAsync(application);

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(StageType.Declaration))
            .ReturnsAsync(new List<StageTaskView>
            {
                new StageTaskView { TaskId = declarationTaskId }
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, StageType.PreEngagement))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.PreEngagement,
                StageName = "Pre-Engagement",
                StatusId = StatusType.Completed,
                Status = "Completed",
                StageStartDate = DateTime.UtcNow.AddDays(-10),
                StageCompletionDate = DateTime.UtcNow.AddDays(-5)
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, StageType.MainApplication))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.MainApplication,
                StageName = "Main Application",
                StatusId = StatusType.Completed,
                Status = "Completed",
                StageStartDate = DateTime.UtcNow.AddDays(-5),
                StageCompletionDate = DateTime.UtcNow.AddDays(-1)
            });

        // Act
        var result = await _service.GetTaskStatusesForApplication(applicationId);

        // Assert
        Assert.NotNull(result);
        var task = result.SelectMany(r => r.Tasks).FirstOrDefault(t => t.TaskId == declarationTaskId);
        Assert.NotNull(task);
        Assert.Equal("Not Yet Released", task!.HintText);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskStatusesForApplication_ShouldReturnNull_WhenNoTaskStatuses()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        _mockTaskStatusRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync((IEnumerable<TaskItemStatusSection>?)null!);

        // Act
        var result = await _service.GetTaskStatusesForApplication(applicationId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskStatusesForApplication_ShouldReturnNull_WhenApplicationNotFound()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        _mockTaskStatusRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(new List<TaskItemStatusSection>
            {
            new TaskItemStatusSection
                {
                    SectionId = Guid.NewGuid(),
                    SectionName = "Section A",
                    SectionOrderNumber = 1,
                    TaskId = Guid.NewGuid(),
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrderNumber = 1,
                    TaskStatusId = Guid.NewGuid(),
                    Status = StatusType.NotStarted,
                    QuestionNameUrl = "q1"
                }
            });

        _mockApplicationRepository
            .Setup(r => r.GetApplicationById(applicationId))
            .ReturnsAsync((Application?)null);

        // Act
        var result = await _service.GetTaskStatusesForApplication(applicationId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DetermineAndCreateTaskStatuses_ReturnsFalse_WhenNoTasks()
    {
        // Arrange
        _mockTaskRepository
            .Setup(r => r.GetAllTask())
            .ReturnsAsync((IEnumerable<TaskItem>?)null!);

        // Act
        var result = await _service.DetermineAndCreateTaskStatuses(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DetermineAndCreateTaskStatuses_ReturnsFalse_WhenNoAnswers()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        _mockTaskRepository
            .Setup(r => r.GetAllTask())
            .ReturnsAsync(new List<TaskItem>
            {
            new TaskItem
                {
                    TaskId = taskId,
                    TaskName = "Example Task",
                    TaskNameUrl = "example-task",
                    TaskOrderNumber = 1,
                    SectionId = Guid.NewGuid(),
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    CreatedByUpn = "test@ofqual.gov.uk"
                }
            });

        _mockApplicationAnswersRepository
            .Setup(r => r.GetAllApplicationAnswers(appId))
            .ReturnsAsync((IEnumerable<SectionTaskQuestionAnswer>?)null!);

        // Act
        var result = await _service.DetermineAndCreateTaskStatuses(appId);

        // Assert
        Assert.False(result);
    }
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DetermineAndCreateTaskStatuses_ReturnsTrue_AndCreatesStatusesCorrectly()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var declarationTaskId = Guid.NewGuid();
        var normalTaskId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _mockTaskRepository
            .Setup(r => r.GetAllTask())
            .ReturnsAsync(new List<TaskItem>
            {
            new TaskItem
                {
                    TaskId = normalTaskId,
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrderNumber = 1,
                    SectionId = Guid.NewGuid(),
                    CreatedDate = now,
                    ModifiedDate = now,
                    CreatedByUpn = "test@ofqual.gov.uk"
                },
            new TaskItem
                {
                    TaskId = declarationTaskId,
                    TaskName = "Declaration",
                    TaskNameUrl = "declaration",
                    TaskOrderNumber = 2,
                    SectionId = Guid.NewGuid(),
                    CreatedDate = now,
                    ModifiedDate = now,
                    CreatedByUpn = "test@ofqual.gov.uk"
                }
            });

        _mockApplicationAnswersRepository
            .Setup(r => r.GetAllApplicationAnswers(appId))
            .ReturnsAsync(new List<SectionTaskQuestionAnswer>
            {
            new SectionTaskQuestionAnswer
                {
                    ApplicationId = appId,
                    QuestionId = questionId,
                    Answer = "{\"some\":\"data\"}",
                    TaskId = normalTaskId,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrderNumber = 1,
                    SectionId = Guid.NewGuid(),
                    SectionName = "Section A",
                    SectionOrderNumber = 1
                }
            });

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(StageType.Declaration))
            .ReturnsAsync(new List<StageTaskView>
            {
            new StageTaskView
            {
                TaskId = declarationTaskId,
                StageId = StageType.Declaration,
                StageName = "Declaration",
                Task = "Declaration",
                OrderNumber = 2
            }
            });

        _mockTaskStatusRepository
            .Setup(r => r.CreateTaskStatuses(It.IsAny<IEnumerable<TaskItemStatus>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DetermineAndCreateTaskStatuses(appId);

        // Assert
        Assert.True(result);

        _mockTaskStatusRepository.Verify(r => r.CreateTaskStatuses(It.Is<IEnumerable<TaskItemStatus>>(statuses =>
            statuses.Count() == 2 &&
            statuses.Any(s => s.TaskId == normalTaskId && s.Status == StatusType.Completed) &&
            statuses.Any(s => s.TaskId == declarationTaskId && s.Status == StatusType.CannotStartYet)
        )), Times.Once);
    }
}