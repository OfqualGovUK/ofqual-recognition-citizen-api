using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Moq;

namespace Ofqual.Recognition.Citizen.Tests.Unit.Controllers;

public class QuestionControllerTests
{
    private readonly QuestionController _controller;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;

    public QuestionControllerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockUnitOfWork.Setup(u => u.QuestionRepository).Returns(_mockQuestionRepository.Object);

        _controller = new QuestionController(_mockUnitOfWork.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("review-submit", "review-your-application")]
    [InlineData("declaration-and-submit", "submit")]
    public async Task GetQuestions_ReturnsQuestion_WhenQuestionExists(string taskName, string questionName)
    {
        // Arrange
        var expectedQuestion = new QuestionDetails
        {
            CurrentQuestionNameUrl = questionName,
            QuestionContent = "{\"hint\":\"test.\"}",
            QuestionTypeName = "File Upload",
            QuestionId = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            TaskNameUrl = taskName
        };

        _mockQuestionRepository
            .Setup(repo => repo.GetQuestionByTaskAndQuestionUrl(taskName, questionName))
            .ReturnsAsync(expectedQuestion);

        // Act
        var result = await _controller.GetQuestions(taskName, questionName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedQuestion = Assert.IsType<QuestionDetailsDto>(okResult.Value);

        Assert.Equal($"{expectedQuestion.TaskNameUrl}/{expectedQuestion.CurrentQuestionNameUrl}", returnedQuestion.CurrentQuestionUrl);
        Assert.Equal(expectedQuestion.QuestionTypeName, returnedQuestion.QuestionTypeName);
        Assert.Equal(expectedQuestion.QuestionContent, returnedQuestion.QuestionContent);
        Assert.Equal(expectedQuestion.QuestionId, returnedQuestion.QuestionId);
        Assert.Equal(expectedQuestion.TaskId, returnedQuestion.TaskId);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("nonexistent", "missingquestion")]
    public async Task GetQuestions_ReturnsBadRequest_WhenQuestionIsNull(string taskName, string questionName)
    {
        // Arrange
        _mockQuestionRepository
            .Setup(repo => repo.GetQuestionByTaskAndQuestionUrl(taskName, questionName))
            .ReturnsAsync((QuestionDetails?)null);

        // Act
        var result = await _controller.GetQuestions(taskName, questionName);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal($"No question found with URL: {taskName}/{questionName}", badRequestResult.Value);
    }
}