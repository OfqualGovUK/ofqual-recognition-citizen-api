using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Controller for recognition citizen
/// </summary>
[ApiController]
[Route("recognition/citizen")]
public class RecognitionCitizenController : ControllerBase
{
    private readonly IUnitOfWork _context;

    /// <summary>
    /// Initialises a new instance of <see cref="RecognitionCitizenController"/>.
    /// </summary>
    public RecognitionCitizenController(IUnitOfWork context)
    {
        _context = context;
    }

    /// <summary>
    /// Creates a new application with initial task statuses.
    /// </summary>
    /// <returns>The created application.</returns>
    [HttpPost("application")]
    public async Task<ActionResult<Application>> CreateApplication()
    {
        try
        {
            var application = await _context.ApplicationRepository.CreateApplication();

            if (application == null) {
                return BadRequest("Application could not be created.");
            }

            _context.Commit();
            return Ok(application);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating a new application.");
            throw new Exception("An error occurred while creating the application. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves tasks and their statuses for a given application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>List of tasks with statuses.</returns>
    [HttpGet("application/{applicationId}/tasks")]
    public async Task<ActionResult<List<TaskStatusDto>>> GetApplicationTasks(Guid applicationId)
    {
        try
        {
            var taskStatusDto = await _context.TaskRepository.GetTasksByApplicationId(applicationId);
            return Ok(taskStatusDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving tasks for application {ApplicationId}.", applicationId);
            throw new Exception("An error occurred while fetching tasks for the application. Please try again later.");
        }
    }

    /// <summary>
    /// Updates a task's status, ensuring the task belongs to the specified application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <param name="taskId">The task ID.</param>
    /// <param name="status">The new status.</param>
    [HttpPost("application/{applicationId}/tasks/{taskId}/status/{status}")]
    public async Task<IActionResult> UpdateTaskStatus(Guid applicationId, Guid taskId, TaskStatusEnum status)
    {
        try
        {
            var isStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, status);

            if (!isStatusUpdated)
            {
                return BadRequest("Failed to update task status. Either the task does not exist or belongs to a different application.");
            }

            _context.Commit();
            return Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while updating the status of task {TaskId} for application {ApplicationId}.", taskId, applicationId);
            throw new Exception("An error occurred while updating the task status. Please try again later.");
        }
    }
}