using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Handles task-related operations within a section.
/// </summary>
[ApiController]
[Route("tasks")]
[Route("questions")]
[Authorize]
[RequiredScope("Applications.ReadWrite")]
public class TaskController : ControllerBase
{
    private readonly IUnitOfWork _context;
    private readonly ITaskService _taskService;

    /// <summary>
    /// Initialises a new instance of <see cref="TaskController"/>.
    /// </summary>
    public TaskController(IUnitOfWork context, ITaskService taskService)
    {
        _context = context;
        _taskService = taskService;
    }

    /// <summary>
    /// Returns task details based on the task URL.
    /// </summary>
    /// <param name="taskNameUrl">URL-formatted task name.</param>
    /// <returns>The task with its details.</returns>
    [HttpGet("{taskNameUrl}")]
    public async Task<ActionResult<TaskItemDto?>> GetTaskByTaskNameUrl(string taskNameUrl)
    {
        try
        {
            TaskItemDto? taskItemDto = await _taskService.GetTaskWithStatusByUrl(taskNameUrl);
            if (taskItemDto == null)
            {
                return BadRequest($"No task found with URL: {taskNameUrl}");
            }

            return Ok(taskItemDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving task for URL: {TaskNameUrl}", taskNameUrl);
            throw new Exception("An error occurred while fetching the task. Please try again later.");
        }
    }
}