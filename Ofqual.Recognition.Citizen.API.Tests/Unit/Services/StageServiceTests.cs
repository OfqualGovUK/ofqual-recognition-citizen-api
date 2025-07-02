using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class StageServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ITaskRepository> _mockTaskRepository = new();
    private readonly Mock<IApplicationAnswersRepository> _mockApplicationAnswersRepository = new();
    private readonly Mock<IStageRepository> _mockStageRepository = new();
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();
    private readonly StageService _stageService;

    public StageServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);
        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository).Returns(_mockApplicationAnswersRepository.Object);
        _mockUnitOfWork.Setup(u => u.StageRepository).Returns(_mockStageRepository.Object);

        _stageService = new StageService(_mockUnitOfWork.Object, _mockUserInformationService.Object);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(StageType.PreEngagement)]
    [InlineData(StageType.MainApplication)]
    [InlineData(StageType.Declaration)]
    public async Task EvaluateAndUpsertAllStageStatus_ReturnsFalse_WhenAnyStageFails(StageType failingStage)
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(failingStage))
            .ReturnsAsync(new List<StageTaskView>());

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(It.Is<StageType>(s => s != failingStage)))
            .ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = Guid.NewGuid() } });

        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(new List<TaskItemStatusSection>
            {
            new TaskItemStatusSection
                {
                    TaskId = Guid.NewGuid(),
                    Status = StatusType.Completed,
                    SectionId = Guid.NewGuid(),
                    SectionName = "Section",
                    SectionOrderNumber = 1,
                    TaskName = "Task",
                    TaskNameUrl = "task-url",
                    TaskOrderNumber = 1,
                    TaskStatusId = Guid.NewGuid(),
                    QuestionNameUrl = "question-url"
                }
            });

        // Act
        var result = await _stageService.EvaluateAndUpsertAllStageStatus(applicationId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EvaluateAndUpsertAllStageStatus_ReturnsTrue_WhenAllStagesSucceed()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var taskId3 = Guid.NewGuid();

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(StageType.PreEngagement))
            .ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = taskId1 } });

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(StageType.MainApplication))
            .ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = taskId2 } });

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(StageType.Declaration))
            .ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = taskId3 } });

        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(new List<TaskItemStatusSection>
            {
            new TaskItemStatusSection
                {
                    TaskId = taskId1,
                    Status = StatusType.Completed,
                    SectionId = Guid.NewGuid(),
                    SectionName = "Section",
                    SectionOrderNumber = 1,
                    TaskName = "Task1",
                    TaskNameUrl = "task1-url",
                    TaskOrderNumber = 1,
                    TaskStatusId = Guid.NewGuid(),
                    QuestionNameUrl = "question1-url"
                },
            new TaskItemStatusSection
                {
                    TaskId = taskId2,
                    Status = StatusType.Completed,
                    SectionId = Guid.NewGuid(),
                    SectionName = "Section",
                    SectionOrderNumber = 1,
                    TaskName = "Task2",
                    TaskNameUrl = "task2-url",
                    TaskOrderNumber = 1,
                    TaskStatusId = Guid.NewGuid(),
                    QuestionNameUrl = "question2-url"
                },
            new TaskItemStatusSection
                {
                    TaskId = taskId3,
                    Status = StatusType.Completed,
                    SectionId = Guid.NewGuid(),
                    SectionName = "Section",
                    SectionOrderNumber = 1,
                    TaskName = "Task3",
                    TaskNameUrl = "task3-url",
                    TaskOrderNumber = 1,
                    TaskStatusId = Guid.NewGuid(),
                    QuestionNameUrl = "question3-url"
                }
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, It.IsAny<StageType>()))
            .ReturnsAsync((StageStatusView?)null);

        _mockStageRepository
            .Setup(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()))
            .ReturnsAsync(true);

        // Act
        var result = await _stageService.EvaluateAndUpsertAllStageStatus(applicationId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EvaluateAndUpsertStageStatus_ReturnsFalse_WhenNoStageTasks()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var stageTasks = new List<StageTaskView>();

        _mockStageRepository.Setup(r => r.GetAllStageTasksByStageId(StageType.PreEngagement))
            .ReturnsAsync(stageTasks);

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(applicationId, StageType.PreEngagement);

        // Assert
        Assert.False(result);
        _mockStageRepository.Verify(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EvaluateAndUpsertStageStatus_ReturnsFalse_WhenNoTaskStatuses()
    {
        // Arrange
        var applicationId = Guid.NewGuid();

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(It.IsAny<StageType>()))
            .ReturnsAsync(new List<StageTaskView>
            {
            new StageTaskView { TaskId = Guid.NewGuid() }
            });

        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync((IEnumerable<TaskItemStatusSection>?)null);

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(applicationId, StageType.PreEngagement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EvaluateAndUpsertStageStatus_DoesNotUpsert_WhenStatusIsUnchanged()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(It.IsAny<StageType>()))
            .ReturnsAsync(new List<StageTaskView>
            {
                new StageTaskView { TaskId = taskId1 },
                new StageTaskView { TaskId = taskId2 }
            });

        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(new List<TaskItemStatusSection>
            {
                new TaskItemStatusSection { TaskId = taskId1, Status = StatusType.Completed, SectionId = Guid.NewGuid(), SectionName = "S", SectionOrderNumber = 1, TaskName = "T", TaskNameUrl = "t", TaskOrderNumber = 1, TaskStatusId = Guid.NewGuid(), QuestionNameUrl = "q" },
                new TaskItemStatusSection { TaskId = taskId2, Status = StatusType.Completed, SectionId = Guid.NewGuid(), SectionName = "S", SectionOrderNumber = 1, TaskName = "T", TaskNameUrl = "t", TaskOrderNumber = 1, TaskStatusId = Guid.NewGuid(), QuestionNameUrl = "q" }
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, It.IsAny<StageType>()))
            .ReturnsAsync(new StageStatusView
            {
                ApplicationId = applicationId,
                StageId = StageType.PreEngagement,
                StatusId = StatusType.Completed,
                StageStartDate = DateTime.UtcNow.AddDays(-1),
                StageCompletionDate = DateTime.UtcNow,
                StageName = "Stage",
                Status = "Completed"
            });

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(applicationId, StageType.PreEngagement);

        // Assert
        Assert.True(result);
        _mockStageRepository.Verify(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EvaluateAndUpsertStageStatus_Upserts_WhenAllTasksCompleted()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(It.IsAny<StageType>()))
            .ReturnsAsync(new List<StageTaskView>
            {
                new StageTaskView { TaskId = taskId1 },
                new StageTaskView { TaskId = taskId2 }
            });

        _mockTaskRepository
            .Setup(r => r.GetTaskStatusesByApplicationId(applicationId))
            .ReturnsAsync(new List<TaskItemStatusSection>
            {
                new TaskItemStatusSection { TaskId = taskId1, Status = StatusType.Completed, SectionId = Guid.NewGuid(), SectionName = "S", SectionOrderNumber = 1, TaskName = "T", TaskNameUrl = "t", TaskOrderNumber = 1, TaskStatusId = Guid.NewGuid(), QuestionNameUrl = "q" },
                new TaskItemStatusSection { TaskId = taskId2, Status = StatusType.Completed, SectionId = Guid.NewGuid(), SectionName = "S", SectionOrderNumber = 1, TaskName = "T", TaskNameUrl = "t", TaskOrderNumber = 1, TaskStatusId = Guid.NewGuid(), QuestionNameUrl = "q" }
            });

        _mockStageRepository
            .Setup(r => r.GetStageStatus(applicationId, It.IsAny<StageType>()))
            .ReturnsAsync((StageStatusView?)null);

        StageStatus? capturedStatus = null;
        _mockStageRepository
            .Setup(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()))
            .Callback<StageStatus>(status => capturedStatus = status)
            .ReturnsAsync(true);

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(applicationId, StageType.PreEngagement);

        // Assert
        Assert.True(result);
        _mockStageRepository.Verify(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()), Times.Once);
        Assert.NotNull(capturedStatus);
        Assert.Equal(StatusType.Completed, capturedStatus!.StatusId);
    }
}