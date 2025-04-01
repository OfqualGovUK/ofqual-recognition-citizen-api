using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Controllers;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Http;
using Moq;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Xunit;

namespace Ofqual.Recognition.Citizen.Tests.Controllers;

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

        _controller = new QuestionController(_mockUnitOfWork.Object);

        _controller.ControllerContext = new ControllerContext();
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
    }

    [Theory]
    [InlineData("review-submit", "review-your-application")]
    [InlineData("declaration-and-submit", "submit")]
    public async Task GetQuestions_ReturnsQuestion_WhenQuestionExists(string taskName, string questionName)
    {
        // Arrange
        var expectedQuestion = new QuestionDto
        {
            QuestionContent = "{\"hint\":\"test.\"}",
            QuestionTypeName = "File Upload"
        };

        _mockQuestionRepository
            .Setup(repo => repo.GetQuestion($"{taskName}/{questionName}"))
            .ReturnsAsync(expectedQuestion);
        
        // Act
        var result = await _controller.GetQuestions(taskName, questionName);

        // Assert
        var okResult = Assert.IsType<ActionResult<QuestionDto?>>(result);
        var value = Assert.IsType<QuestionDto>(okResult.Value);
        Assert.Equal(expectedQuestion.QuestionTypeName, value.QuestionTypeName);
        Assert.Equal(expectedQuestion.QuestionContent, value.QuestionContent);
    }
    
    [Theory]
    [InlineData("nonexistent", "missingquestion")]
    public async Task GetQuestions_ReturnsBadRequest_WhenQuestionIsNull(string taskName, string questionName)
    {
        // Arrange
        _mockQuestionRepository
            .Setup(repo => repo.GetQuestion($"{taskName}/{questionName}"))
            .ReturnsAsync((QuestionDto?)null);
        
        // Act
        var result = await _controller.GetQuestions(taskName, questionName);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal($"No question found with URL: {taskName}/{questionName}", badRequestResult.Value);
    }
}