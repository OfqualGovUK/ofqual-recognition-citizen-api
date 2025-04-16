using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Handles task-related operations within a section.
/// </summary>
[ApiController]
[Route("tasks")]
public class TaskController : ControllerBase
{
    private readonly IUnitOfWork _context;

    /// <summary>
    /// Initialises a new instance of <see cref="TaskController"/>.
    /// </summary>
    public TaskController(IUnitOfWork context)
    {
        _context = context;
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
            TaskItem? taskItem = await _context.TaskRepository.GetTaskByTaskNameUrl(taskNameUrl);

            if (taskItem == null)
            {
                return BadRequest($"No task found with URL: {taskNameUrl}");
            }

            TaskItemDto taskItemDto = TaskMapper.ToDto(taskItem);

            return Ok(taskItemDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving task for URL: {TaskNameUrl}", taskNameUrl);
            throw new Exception("An error occurred while fetching the task. Please try again later.");
        }
    }
}