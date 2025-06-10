using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class StageServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IApplicationAnswersRepository> _mockApplicationAnswersRepository;
    private readonly Mock<IStageRepository> _mockStageRepository;
    private readonly StageService _stageService;

    public StageServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);

        _mockApplicationAnswersRepository = new Mock<IApplicationAnswersRepository>();
        _mockUnitOfWork.Setup(u => u.ApplicationAnswersRepository).Returns(_mockApplicationAnswersRepository.Object);

        _mockStageRepository = new Mock<IStageRepository>();
        _mockUnitOfWork.Setup(u => u.StageRepository).Returns(_mockStageRepository.Object);

        _stageService = new StageService(_mockUnitOfWork.Object);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpsertStageStatusRecord_ReturnsFalse_WhenNoStageTasks()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var stageTasks = new List<StageTaskView>(); // No tasks for the stage
        var questions = new List<Question>(); // No questions available
        var answers = new List<TaskQuestionAnswer>(); // No answers available
        var stageStatus = new StageStatus
        {
            ApplicationId = applicationId,
            StageId = Stage.PreEngagement,
            StatusId = TaskStatusEnum.InProgress,
            StageStartDate = DateTime.UtcNow.AddDays(-1),
            StageCompletionDate = DateTime.UtcNow,
            CreatedByUpn = "USER"
        };

        _mockStageRepository.Setup(r => r.GetAllStageTasksByStageId(Stage.PreEngagement))
            .ReturnsAsync(stageTasks);

        _mockQuestionRepository.Setup(r => r.GetAllQuestions())
            .ReturnsAsync(questions);

        _mockApplicationAnswersRepository.Setup(r => r.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockStageRepository.Setup(r => r.GetStageStatus(applicationId, Stage.PreEngagement))
            .ReturnsAsync(stageStatus);

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(Guid.NewGuid(), Stage.PreEngagement);

        // Assert
        Assert.False(result);
        _mockStageRepository.Verify(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReturnsFalse_WhenApplicationIdIsEmpty()
    {
        // Arrange
        var applicationId = Guid.Empty;

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement);
        
        // Assert
        Assert.False(result);
        _mockStageRepository.Verify(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EvaluateAndUpsertStageStatus_ReturnsFalse_WhenNoQuestions()
    {
        // Arrange
        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(It.IsAny<Stage>()))
            .ReturnsAsync(new List<StageTaskView> { new StageTaskView { TaskId = Guid.NewGuid() } });

        _mockQuestionRepository
            .Setup(r => r.GetAllQuestions())
            .ReturnsAsync((IEnumerable<Question>?)null!);

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(Guid.NewGuid(), Stage.PreEngagement);

        // Assert
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DoesNotUpsertStatus_WhenStatusIsUnchanged()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();

        var stageTasks = new List<StageTaskView>
            {
                new StageTaskView { TaskId = taskId1, StageId = Stage.PreEngagement, OrderNumber = 1 },
                new StageTaskView { TaskId = taskId2, StageId = Stage.PreEngagement, OrderNumber = 2 }
            };

        var questions = new List<Question>
            {
                new Question
                {
                    QuestionId = questionId1,
                    TaskId = taskId1,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    QuestionOrderNumber = 1,
                    QuestionTypeId = Guid.NewGuid(),
                    CreatedByUpn = "test@ofqual.gov.uk"
                },
                new Question
                {
                    QuestionId = questionId2,
                    TaskId = taskId2,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    QuestionOrderNumber = 1,
                    QuestionTypeId = Guid.NewGuid(),
                    CreatedByUpn = "test@ofqual.gov.uk"
                }
            };

        var answers = new List<TaskQuestionAnswer>
            {
                new TaskQuestionAnswer
                {
                    TaskId  = taskId1,
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrder = 1,
                    QuestionId = questionId1,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    Answer = "{\"some\":\"data\"}",
                    ApplicationId = applicationId
                },
                new TaskQuestionAnswer {

                    TaskId  = taskId2,
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrder = 1,
                    QuestionId = questionId2,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    Answer = "{\"some\":\"data\"}",
                    ApplicationId = applicationId
                }
            };

        var stageStatus = new StageStatus
        {
            ApplicationId = applicationId,
            StageId = Stage.PreEngagement,
            StatusId = TaskStatusEnum.Completed,
            StageStartDate = DateTime.UtcNow.AddDays(-1),
            StageCompletionDate = DateTime.UtcNow,
            CreatedByUpn = "USER"
        };

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(It.IsAny<Stage>()))
            .ReturnsAsync(stageTasks);

        _mockQuestionRepository
            .Setup(r => r.GetAllQuestions())
            .ReturnsAsync(questions);

        _mockApplicationAnswersRepository
            .Setup(r => r.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        // Mock: The existing stage status has been set to Completed
        _mockStageRepository.Setup(r => r.GetStageStatus(applicationId, It.IsAny<Stage>()))
            .ReturnsAsync(stageStatus);

        // Act
        // Evaluate the attempt to Upsert the stage status
        var result = await _stageService.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement);

        // Assert
        // The result should be true, but no Upsert should occur since the status is unchanged
        Assert.True(result);
        // Verify that UpsertStageStatusRecord was never called
        _mockStageRepository.Verify(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpsertStatus_WhenAllTasks_MarkedCompleted()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var questionId1 = Guid.NewGuid();
        var questionId2 = Guid.NewGuid();

        var stageTasks = new List<StageTaskView>
            {
                new StageTaskView { TaskId = taskId1, StageId = Stage.PreEngagement, OrderNumber = 1 },
                new StageTaskView { TaskId = taskId2, StageId = Stage.PreEngagement, OrderNumber = 2 }
            };

        var questions = new List<Question>
            {
                new Question
                {
                    QuestionId = questionId1,
                    TaskId = taskId1,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    QuestionOrderNumber = 1,
                    QuestionTypeId = Guid.NewGuid(),
                    CreatedByUpn = "test@ofqual.gov.uk"
                },
                new Question
                {
                    QuestionId = questionId2,
                    TaskId = taskId2,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    QuestionOrderNumber = 1,
                    QuestionTypeId = Guid.NewGuid(),
                    CreatedByUpn = "test@ofqual.gov.uk"
                }
            };

        var answers = new List<TaskQuestionAnswer>
            {
                new TaskQuestionAnswer
                {
                    TaskId  = taskId1,
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrder = 1,
                    QuestionId = questionId1,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    Answer = "{\"some\":\"data\"}",
                    ApplicationId = applicationId
                },
                new TaskQuestionAnswer {

                    TaskId  = taskId2,
                    TaskName = "Task A",
                    TaskNameUrl = "task-a",
                    TaskOrder = 1,
                    QuestionId = questionId2,
                    QuestionContent = "What is A?",
                    QuestionNameUrl = "question-a",
                    Answer = "{\"some\":\"data\"}",
                    ApplicationId = applicationId
                }
            };

        _mockStageRepository
            .Setup(r => r.GetAllStageTasksByStageId(It.IsAny<Stage>()))
            .ReturnsAsync(stageTasks);

        _mockQuestionRepository
            .Setup(r => r.GetAllQuestions())
            .ReturnsAsync(questions);

        _mockApplicationAnswersRepository
            .Setup(r => r.GetAllApplicationAnswers(applicationId))
            .ReturnsAsync(answers);

        _mockStageRepository.Setup(r => r.GetStageStatus(applicationId, It.IsAny<Stage>()))
            .ReturnsAsync((StageStatus?)null);

        StageStatus stageStatus = null!
        ;
        _mockStageRepository
            .Setup(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()))
            .Callback<StageStatus>(status => stageStatus = status)
            .ReturnsAsync(true);

        // Act
        var result = await _stageService.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement);

        // Assert
        Assert.True(result);
        _mockStageRepository.Verify(r => r.UpsertStageStatusRecord(It.IsAny<StageStatus>()), Times.Once);
        Assert.NotNull(stageStatus);
        Assert.Equal(TaskStatusEnum.Completed, stageStatus.StatusId);
    }
}
