using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Services;

public class TaskStatusServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ITaskRepository> _mockTaskRepository;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly TaskStatusService _service;

    public TaskStatusServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockTaskRepository = new Mock<ITaskRepository>();
        _mockUnitOfWork.Setup(u => u.TaskRepository).Returns(_mockTaskRepository.Object);

        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);
        
        _service = new TaskStatusService(_mockUnitOfWork.Object);
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