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
    private readonly Mock<IQuestionRepository> _mockQuestionRepository = new();
    private readonly Mock<IApplicationRepository> _mockApplicationRepository = new();
    private readonly Mock<IStageRepository> _mockStageRepository = new();
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();
    private readonly Mock<IStageService> _mockStageService = new();
    private readonly TaskStatusService _service;

    public TaskStatusServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);
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
    public async Task UpdateTaskAndStageStatus_Should_Return_True_When_Task_Updated_And_Stage_Updated()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService.Setup(s => s.GetCurrentUserUpn()).Returns(upn);
        _mockTaskRepository.Setup(r =>
            r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.Completed, upn))
            .ReturnsAsync(true);
        _mockStageService.Setup(s =>
            s.EvaluateAndUpsertStageStatus(applicationId, StageType.PreEngagement))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.Completed, StageType.PreEngagement);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskAndStageStatus_Should_Return_True_When_Task_Updated_And_Stage_Not_Required()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService.Setup(s => s.GetCurrentUserUpn()).Returns(upn);
        _mockTaskRepository.Setup(r =>
            r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress, upn))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.InProgress, StageType.PreEngagement);

        // Assert
        Assert.True(result);
        _mockStageService.Verify(s => s.EvaluateAndUpsertStageStatus(It.IsAny<Guid>(), It.IsAny<StageType>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskAndStageStatus_Should_Return_False_When_Task_Update_Fails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService.Setup(s => s.GetCurrentUserUpn()).Returns(upn);
        _mockTaskRepository.Setup(r =>
            r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress, upn))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.InProgress, StageType.PreEngagement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateTaskAndStageStatus_Should_Return_False_When_Stage_Update_Fails()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var upn = "user@ofqual.gov.uk";

        _mockUserInformationService.Setup(s => s.GetCurrentUserUpn()).Returns(upn);
        _mockTaskRepository.Setup(r =>
            r.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.Completed, upn))
            .ReturnsAsync(true);
        _mockStageService.Setup(s =>
            s.EvaluateAndUpsertStageStatus(applicationId, StageType.PreEngagement))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.Completed, StageType.PreEngagement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskStatusesForApplication_ShouldReturnTasksWithCorrectHints()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var declarationTaskId = Guid.NewGuid();
        var informationTaskId = Guid.NewGuid();

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
                Status = TaskStatusEnum.CannotStartYet,
                QuestionNameUrl = "q1"
            },
            new TaskItemStatusSection
            {
                SectionId = Guid.NewGuid(),
                SectionName = "Section B",
                SectionOrderNumber = 2,
                TaskId = informationTaskId,
                TaskName = "Info Task",
                TaskNameUrl = "info-task",
                TaskOrderNumber = 1,
                TaskStatusId = Guid.NewGuid(),
                Status = TaskStatusEnum.Completed,
                QuestionNameUrl = "q2"
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

        _mockTaskRepository.Setup(r => r.GetTaskStatusesByApplicationId(applicationId)).ReturnsAsync(taskStatuses);
        _mockApplicationRepository.Setup(r => r.GetApplicationById(applicationId)).ReturnsAsync(application);
        _mockStageRepository.Setup(r => r.GetAllStageTasksByStageId(StageType.Declaration)).ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = declarationTaskId } });
        _mockStageRepository.Setup(r => r.GetAllStageTasksByStageId(StageType.Information)).ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = informationTaskId } });

        // Act
        var result = await _service.GetTaskStatusesForApplication(applicationId);

        // Assert
        Assert.NotNull(result);
        var dtoList = result.ToList();
        Assert.Contains(dtoList.SelectMany(s => s.Tasks), t => t.TaskId == declarationTaskId && t.Hint == "You must complete all sections first");
        Assert.Contains(dtoList.SelectMany(s => s.Tasks), t => t.TaskId == informationTaskId && t.Hint == "Learn more about the application before you apply");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskStatusesForApplication_ShouldReturnNull_WhenNoTaskStatuses()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        _mockTaskRepository.Setup(r => r.GetTaskStatusesByApplicationId(applicationId)).ReturnsAsync((IEnumerable<TaskItemStatusSection>?)null!);

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

        _mockTaskRepository.Setup(r => r.GetTaskStatusesByApplicationId(applicationId)).ReturnsAsync(new List<TaskItemStatusSection>
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
                Status = TaskStatusEnum.NotStarted,
                QuestionNameUrl = "q1"
            }
        });

        _mockApplicationRepository.Setup(r => r.GetApplicationById(applicationId)).ReturnsAsync((Application?)null);

        // Act
        var result = await _service.GetTaskStatusesForApplication(applicationId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetTaskStatusesForApplication_ShouldReturnNotYetReleasedHint_IfSubmitted()
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
                Status = TaskStatusEnum.CannotStartYet,
                QuestionNameUrl = "q1"
            }
        };

        var application = new Application
        {
            ApplicationId = applicationId,
            OwnerUserId = Guid.NewGuid(),
            SubmittedDate = DateTime.UtcNow.AddDays(-1),
            ApplicationReleaseDate = DateTime.UtcNow.AddDays(2),
            OrganisationId = Guid.NewGuid(),
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk",
            ModifiedByUpn = "test@ofqual.gov.uk"
        };

        _mockTaskRepository.Setup(r => r.GetTaskStatusesByApplicationId(applicationId)).ReturnsAsync(taskStatuses);
        _mockApplicationRepository.Setup(r => r.GetApplicationById(applicationId)).ReturnsAsync(application);
        _mockStageRepository.Setup(r => r.GetAllStageTasksByStageId(StageType.Declaration)).ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = declarationTaskId } });
        _mockStageRepository.Setup(r => r.GetAllStageTasksByStageId(StageType.Information)).ReturnsAsync(Enumerable.Empty<StageTaskView>());

        // Act
        var result = await _service.GetTaskStatusesForApplication(applicationId);

        // Assert
        Assert.NotNull(result);
        var task = result.SelectMany(r => r.Tasks).FirstOrDefault(t => t.TaskId == declarationTaskId);
        Assert.NotNull(task);
        Assert.Equal("Not Yet Released", task!.Hint);
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
        var result = await _service.DetermineAndCreateTaskStatuses(Guid.NewGuid(), null);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DetermineAndCreateTaskStatuses_ReturnsFalse_WhenNoQuestions()
    {
        // Arrange
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

        _mockQuestionRepository
            .Setup(r => r.GetAllQuestions())
            .ReturnsAsync((IEnumerable<Question>?)null!);

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(StageType.Declaration))
            .ReturnsAsync(new List<StageTaskView>());

        // Act
        var result = await _service.DetermineAndCreateTaskStatuses(Guid.NewGuid(), null);

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

        _mockQuestionRepository
            .Setup(r => r.GetAllQuestions())
            .ReturnsAsync(new List<Question>
            {
            new Question
            {
                QuestionId = questionId,
                TaskId = normalTaskId,
                QuestionContent = "What is A?",
                QuestionNameUrl = "question-a",
                QuestionOrderNumber = 1,
                QuestionTypeId = Guid.NewGuid(),
                CreatedByUpn = "test@ofqual.gov.uk"
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

        var answers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto { QuestionId = questionId, AnswerJson = "{\"some\":\"data\"}" }
        };

        _mockTaskRepository
            .Setup(r => r.CreateTaskStatuses(It.IsAny<IEnumerable<TaskItemStatus>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DetermineAndCreateTaskStatuses(appId, answers);

        // Assert
        Assert.True(result);

        _mockTaskRepository.Verify(r => r.CreateTaskStatuses(It.Is<IEnumerable<TaskItemStatus>>(statuses =>
            statuses.Count() == 2 &&
            statuses.Any(s => s.TaskId == normalTaskId && s.Status == TaskStatusEnum.Completed) &&
            statuses.Any(s => s.TaskId == declarationTaskId && s.Status == TaskStatusEnum.CannotStartYet)
        )), Times.Once);
    }
}