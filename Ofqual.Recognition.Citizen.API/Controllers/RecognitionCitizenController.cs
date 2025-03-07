namespace Ofqual.Recognition.Citizen.API.Controllers;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for recognition citizen
/// </summary>
[ApiController]
[Route("recognition/citizen")]
public class RecognitionCitizenController : ControllerBase
{
    /// <summary>
    /// Initialises a new instance of <see cref="RecognitionCitizenController"/>.
    /// </summary>
    public RecognitionCitizenController()
    {
    }

    /// <summary>
    /// Creates a new application with initial task statuses.
    /// </summary>
    /// <returns>The created application.</returns>
    [HttpPost("application")]
    public IActionResult CreateApplication()
    {
        return Ok();
    }

    /// <summary>
    /// Retrieves tasks and their statuses for a given application.
    /// Completed tasks include their answers.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>List of tasks with statuses and answers.</returns>
    [HttpGet("application/{applicationId}/tasks")]
    public IActionResult GetApplicationTasks(Guid applicationId)
    {
        return Ok();
    }

    /// <summary>
    /// Updates a task's status, ensuring the task belongs to the specified application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="taskId">The task ID.</param>
    /// <param name="status">The new status.</param>
    [HttpPost("application/{applicationId}/tasks/{taskId}/status/{status}")]
    public IActionResult UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatus status)
    {
        return Ok();
    }
}