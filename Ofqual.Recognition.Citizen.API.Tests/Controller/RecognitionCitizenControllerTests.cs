using Xunit;
using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Controllers;

namespace Ofqual.Recognition.Citizen.Tests.Controllers
{
    public class RecognitionCitizenControllerTests
    {
        private readonly RecognitionCitizenController _controller;
        public RecognitionCitizenControllerTests()
        {
            _controller = new RecognitionCitizenController();
        }

        [Fact]
        public void CreateApplication_ShouldReturnOk()
        {
            // Act
            var result = _controller.CreateApplication();
            // Assert
            var okResult = Assert.IsType<OkResult>(result);
        }

        [Fact]
        public void GetApplicationTasks_ShouldReturnOk_WhenValidApplicationIdProvided()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            // Act
            var result = _controller.GetApplicationTasks(applicationId);
            // Assert
            var okResult = Assert.IsType<OkResult>(result);
        }

        [Fact]
        public void UpdateTaskStatus_ShouldReturnOk_WhenValidParametersProvided()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var taskId = Guid.NewGuid();
            var status = TaskStatus.Created;
            // Act
            var result = _controller.UpdateTaskStatus(applicationId, taskId, status);
            // Assert
            var okResult = Assert.IsType<OkResult>(result);
        }
    }
}