using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Controllers;

public class PreEngagementControllerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IStageRepository> _mockStageRepository;
    private readonly PreEngagementController _controller;

    public PreEngagementControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockStageRepository = new Mock<IStageRepository>();
        _mockUnitOfWork.Setup(u => u.StageRepository).Returns(_mockStageRepository.Object);

        _controller = new PreEngagementController(_mockUnitOfWork.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetFirstPreEngagementQuestion_Should_ReturnOk_WhenQuestionExists()
    {
        // Arrange
        var questionDto = new StageQuestionDto
        {
            CurrentTaskNameUrl = "task-a",
            CurrentQuestionNameUrl = "question-a"
        };

        _mockStageRepository.Setup(r => r.GetFirstQuestionByStage(StageEnum.PreEngagement)).ReturnsAsync(questionDto);

        // Act
        var result = await _controller.GetFirstPreEngagementQuestion();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(questionDto, okResult.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetFirstPreEngagementQuestion_Should_ReturnNotFound_WhenQuestionIsNull()
    {
        // Arrange
        _mockStageRepository.Setup(r => r.GetFirstQuestionByStage(StageEnum.PreEngagement)).ReturnsAsync((StageQuestionDto?)null);

        // Act
        var result = await _controller.GetFirstPreEngagementQuestion();

        // Assert
        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("No Pre-Engagement question found.", notFound.Value);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPreEngagementQuestions_Should_ReturnOk_WhenQuestionExists()
    {
        // Arrange
        var question = new StageQuestionDetails
        {
            QuestionId = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            QuestionContent = "{\"title\":\"test\"}",
            CurrentQuestionNameUrl = "question-b",
            CurrentTaskNameUrl = "task-b",
            QuestionTypeName = "Text",
            NextQuestionNameUrl = "next-q",
            NextTaskNameUrl = "next-t",
            PreviousQuestionNameUrl = "prev-q",
            PreviousTaskNameUrl = "prev-t"
        };

        _mockStageRepository.Setup(r => r.GetStageQuestionByTaskAndQuestionUrl(StageEnum.PreEngagement, "task-b", "question-b")).ReturnsAsync(question);

        // Act
        var result = await _controller.GetPreEngagementQuestions("task-b", "question-b");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<QuestionDetailsDto>(okResult.Value);
        Assert.Equal(question.QuestionId, dto.QuestionId);
        Assert.Equal(question.TaskId, dto.TaskId);
        Assert.Equal(question.QuestionContent, dto.QuestionContent);
        Assert.Equal(question.QuestionTypeName, dto.QuestionTypeName);
        Assert.Equal("task-b/question-b", dto.CurrentQuestionUrl);
        Assert.Equal("next-t/next-q", dto.NextQuestionUrl);
        Assert.Equal("prev-t/prev-q", dto.PreviousQuestionUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPreEngagementQuestions_Should_ReturnBadRequest_WhenQuestionIsNull()
    {
        // Arrange
        _mockStageRepository.Setup(r => r.GetStageQuestionByTaskAndQuestionUrl(StageEnum.PreEngagement, "missing-task", "missing-question"))
                         .ReturnsAsync((StageQuestionDetails?)null);

        // Act
        var result = await _controller.GetPreEngagementQuestions("missing-task", "missing-question");

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("No pre-engagement question found for URL: missing-task/missing-question", badRequest.Value);
    }
}