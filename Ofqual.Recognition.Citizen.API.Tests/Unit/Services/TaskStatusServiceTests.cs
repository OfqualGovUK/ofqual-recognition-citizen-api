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
    private readonly Mock<IUserInformationService> _mockUserInformationService = new();
    private readonly Mock<IStageService> _mockStageService = new();
    private readonly TaskStatusService _service;

    public TaskStatusServiceTests()
    {
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);

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
            s.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.Completed, Stage.PreEngagement);

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
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.InProgress, Stage.PreEngagement);

        // Assert
        Assert.True(result);
        _mockStageService.Verify(s => s.EvaluateAndUpsertStageStatus(It.IsAny<Guid>(), It.IsAny<Stage>()), Times.Never);
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
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.InProgress, Stage.PreEngagement);

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
            s.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement))
            .ReturnsAsync(false);

        // Act
        var result = await _service.UpdateTaskAndStageStatus(applicationId, taskId, TaskStatusEnum.Completed, Stage.PreEngagement);

        // Assert
        Assert.False(result);
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
        _mockTaskRepository
            .Setup(r => r.GetAllTask())
            .ReturnsAsync(new List<TaskItem>
            {
                new TaskItem
                {
                    TaskId = Guid.NewGuid(),
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
        var taskId = Guid.NewGuid();
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();
        var now = DateTime.UtcNow;

        _mockTaskRepository
            .Setup(r => r.GetAllTask())
            .ReturnsAsync(new List<TaskItem>
            {
                new TaskItem
                {
                    TaskId = taskId,
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrderNumber = 1,
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
                    QuestionId = questionId1,
                    TaskId = taskId,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    QuestionOrderNumber = 1,
                    QuestionTypeId = Guid.NewGuid(),
                    CreatedByUpn = "test@ofqual.gov.uk"
                },
                new Question
                {
                    QuestionId = questionId2,
                    TaskId = taskId,
                    QuestionContent = "What is B?",
                    QuestionNameUrl = "question-b",
                    QuestionOrderNumber = 2,
                    QuestionTypeId = Guid.NewGuid(),
                    CreatedByUpn = "test@ofqual.gov.uk"
                }
            });

        var answers = new List<PreEngagementAnswerDto>
        {
            new PreEngagementAnswerDto { QuestionId = questionId1, AnswerJson = "{\"some\":\"data\"}" }
        };

        _mockTaskRepository
            .Setup(r => r.CreateTaskStatuses(It.IsAny<IEnumerable<TaskItemStatus>>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DetermineAndCreateTaskStatuses(appId, answers);

        // Assert
        Assert.True(result);

        _mockTaskRepository.Verify(r => r.CreateTaskStatuses(It.Is<IEnumerable<TaskItemStatus>>(statuses =>
            statuses.Count() == 1 &&
            statuses.First().ApplicationId == appId &&
            statuses.First().TaskId == taskId &&
            statuses.First().Status == TaskStatusEnum.InProgress
        )), Times.Once);
    }
}