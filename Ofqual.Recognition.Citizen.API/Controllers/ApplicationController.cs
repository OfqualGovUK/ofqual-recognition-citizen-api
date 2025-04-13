using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ICheckYourAnswersService _checkYourAnswersService;

    /// <summary>
    /// Initialises a new instance of <see cref="ApplicationController"/>.
    /// </summary>
    public ApplicationController(IUnitOfWork context, ICheckYourAnswersService checkYourAnswersService)
    {
        _context = context;
        _checkYourAnswersService = checkYourAnswersService;
    }

    /// <summary>
    /// Creates a new application with initial task statuses.
    /// </summary>
    /// <returns>The created application.</returns>
    [HttpPost]
    public async Task<ActionResult<ApplicationDetailsDto>> CreateApplication()
    {
        try
        {
            Application? application = await _context.ApplicationRepository.CreateApplication();

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

            ApplicationDetailsDto applicationDetailsDto = ApplicationMapper.MapToApplicationDetailsDto(application);

            _context.Commit();
            return Ok(applicationDetailsDto);
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
            var taskStatuses = await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId);

            if (taskStatuses == null || !taskStatuses.Any())
            {
                return BadRequest("No tasks found for the specified application.");
            }

            var taskItemStatusSectionList = TaskMapper.MapToSectionsWithTasks(taskStatuses);

            return Ok(taskItemStatusSectionList);
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
    public async Task<IActionResult> UpdateTaskStatus(Guid applicationId, Guid taskId, [FromBody] UpdateTaskStatusDto request)
    {
        try
        {
            bool isStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, request.Status);

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

    /// <summary>
    /// Submits an answer to a specific task question.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="taskId">The ID of the task.</param>
    /// <param name="questionId">The ID of the question being answered.</param>
    /// <param name="request">The answer payload.</param>
    [HttpPost("{applicationId}/tasks/{taskId}/questions/{questionId}")]
    public async Task<ActionResult<QuestionAnswerSubmissionResponseDto?>> SubmitQuestionAnswer(Guid applicationId, Guid taskId, Guid questionId, [FromBody] QuestionAnswerSubmissionDto request)
    {
        try
        {
            bool isAnswerInserted = await _context.QuestionRepository.InsertQuestionAnswer(applicationId, questionId, request.Answer);

            if (!isAnswerInserted)
            {
                return BadRequest("Failed to save the question answer. Please check your input and try again.");
            }

            bool isStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress);

            if (!isStatusUpdated)
            {
                return BadRequest("Failed to update task status. Either the task does not exist or belongs to a different application.");
            }

            QuestionAnswerSubmissionResponseDto? redirectUrl = await _context.QuestionRepository.GetNextQuestionUrl(questionId);

            _context.Commit();
            return Ok(redirectUrl);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while inserting an answer for QuestionId: {QuestionId} in ApplicationId: {ApplicationId}.", questionId, applicationId);
            throw new Exception("An error occurred while saving the answer. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves all question answers for a specific task in an application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="taskId">The ID of the task.</param>
    [HttpGet("{applicationId}/tasks/{taskId}/questions/answers")]
    public async Task<ActionResult<List<QuestionAnswerReviewDto>>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId)
    {
        try
        {
            var taskQuestionAnswers = await _context.QuestionRepository.GetTaskQuestionAnswers(applicationId, taskId);

            if (!taskQuestionAnswers.Any())
            {
                return NotFound("No question answers found for the specified task and application.");
            }

            var reviewAnswers = _checkYourAnswersService.GetQuestionAnswers(taskQuestionAnswers);

            return Ok(reviewAnswers);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving question answers for TaskId: {TaskId} and ApplicationId: {ApplicationId}.", taskId, applicationId);
            throw new Exception("An error occurred while fetching the question answers. Please try again later.");
        }
    }
}