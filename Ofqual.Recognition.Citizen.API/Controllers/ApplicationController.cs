using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Controller for recognition citizen application
/// </summary>
[ApiController]
[Route("applications")]
public class ApplicationController : ControllerBase
{
    private readonly IUnitOfWork _context;
    private readonly ITaskService _taskService;

    /// <summary>
    /// Initialises a new instance of <see cref="ApplicationController"/>.
    /// </summary>
    public ApplicationController(IUnitOfWork context, ITaskService taskService)
    {
        _context = context;
        _taskService = taskService;
    }

    /// <summary>
    /// Creates a new application with initial task statuses.
    /// </summary>
    /// <returns>The created application.</returns>
    [HttpPost]
    public async Task<ActionResult<ApplicationDto>> CreateApplication()
    {
        try
        {
            var application = await _context.ApplicationRepository.CreateApplication();

            if (application == null)
            {
                return BadRequest("Application could not be created.");
            }

            var tasks = await _context.TaskRepository.GetAllTask();

            if (tasks == null || !tasks.Any())
            {
                return BadRequest("No tasks found to create statuses for the application.");
            }

            // Create Task Statuses for all Tasks
            bool isTaskStatusesCreated = await _context.TaskRepository.CreateTaskStatuses(application.ApplicationId, tasks);

            if (!isTaskStatusesCreated)
            {
                return BadRequest("Failed to create task statuses for the new application.");
            }

            var applicationDto = ApplicationMapper.MapToApplicationDto(application); 

            _context.Commit();

            return Ok(applicationDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while creating a new application.");
            throw new Exception("An error occurred while creating the application. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves sections with tasks and their statuses for a given application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>A list of sections containing tasks with statuses.</returns>
    [HttpGet("{applicationId}/tasks")]
    public async Task<ActionResult<List<TaskItemStatusSectionDto>>> GetApplicationTasks(Guid applicationId)
    {
        try
        {
            var tasks = await _taskService.GetSectionsWithTasksByApplicationId(applicationId);

            return Ok(tasks);
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
    [HttpPost("{applicationId}/tasks/{taskId}")]
    public async Task<IActionResult> UpdateTaskStatus(Guid applicationId, Guid taskId, [FromBody] TaskStatusEnum status)
    {
        try
        {
            bool isStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, status);

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