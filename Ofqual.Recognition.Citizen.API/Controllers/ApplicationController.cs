using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Attributes;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Controller for recognition citizen application
/// </summary>
[ApiController]
[Route("applications")]
[Authorize]
[RequiredScope("Applications.ReadWrite")]
public class ApplicationController : ControllerBase
{
    private readonly IUnitOfWork _context;
    private readonly ITaskStatusService _taskStatusService;
    private readonly IStageService _stageService;
    private readonly IApplicationAnswersService _applicationAnswersService;
    private readonly IGovUkNotifyService _govUkNotifyService;
    private readonly IApplicationService _applicationService;

    /// <summary>
    /// Initialises a new instance of <see cref="ApplicationController"/>.
    /// </summary>
    public ApplicationController(
        IUnitOfWork context,
        ITaskStatusService taskStatusService,
        IApplicationAnswersService applicationAnswersService,
        IStageService stageService,
        IGovUkNotifyService govUkNotifyService,
        IApplicationService applicationService
        )
    {
        _context = context;
        _taskStatusService = taskStatusService;
        _applicationAnswersService = applicationAnswersService;
        _stageService = stageService;
        _govUkNotifyService = govUkNotifyService;
        _applicationService = applicationService;
    }

    /// <summary>
    /// Initialises a new application or returns the latest one for the current user.
    /// </summary>
    /// <param name="PreEngagementAnswers">Optional pre-engagement answers.</param>
    /// <returns>The initialised or existing application.</returns>
    [HttpPost]
    public async Task<ActionResult<ApplicationDetailsDto>> InitialiseApplication([FromBody] IEnumerable<PreEngagementAnswerDto>? PreEngagementAnswers)
    {
        try
        {
            ApplicationDetailsDto? latestApplication = await _applicationService.GetLatestApplicationForCurrentUser();
            if (latestApplication != null)
            {
                return Ok(latestApplication);
            }

            Application? application = await _applicationService.CreateApplicationForCurrentUser();
            if (application == null)
            {
                return BadRequest("Application could not be created.");
            }

            if (PreEngagementAnswers != null && PreEngagementAnswers.Any())
            {
                bool isPreEngagementAnswersInserted = await _applicationAnswersService.SavePreEngagementAnswers(application.ApplicationId, PreEngagementAnswers);
                if (!isPreEngagementAnswersInserted)
                {
                    return BadRequest("Failed to insert pre-engagement answers for the new application.");
                }
            }

            bool taskStatusesCreated = await _taskStatusService.DetermineAndCreateTaskStatuses(application.ApplicationId);
            if (!taskStatusesCreated)
            {
                return BadRequest("Failed to create task statuses for the new application.");
            }

            bool stageStatusUpdated = await _stageService.EvaluateAndUpsertAllStageStatus(application.ApplicationId);
            if (!stageStatusUpdated)
            {
                return BadRequest("Unable to determine or save the stage status for the application.");
            }

            await _govUkNotifyService.SendEmailAccountCreation();

            ApplicationDetailsDto applicationDetailsDto = ApplicationMapper.ToDto(application);

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
    /// Retrieves the latest application details for the current user.
    /// </summary>
    /// <returns>The latest application details.</returns>
    [HttpGet]
    public async Task<ActionResult<ApplicationDetailsDto>> GetLatestApplicationDetails()
    {
        try
        {
            ApplicationDetailsDto? latestApplication = await _applicationService.GetLatestApplicationForCurrentUser();
            if (latestApplication == null)
            {
                return NotFound("Application not found");
            }

            return Ok(latestApplication);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving the latest application details.");
            throw new Exception("An error occurred while fetching the latest application details. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves sections with tasks and their statuses for a given application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    /// <returns>A list of sections containing tasks with statuses.</returns>
    [HttpGet("{applicationId}/tasks")]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<ActionResult<List<TaskItemStatusSectionDto>>> GetApplicationTasks(Guid applicationId)
    {
        try
        {
            var taskStatuses = await _taskStatusService.GetTaskStatusesForApplication(applicationId);
            if (taskStatuses == null)
            {
                return NotFound("No tasks found for the specified application.");
            }

            return Ok(taskStatuses);
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
    [CheckApplicationId(queryParam: "applicationId")]
    [PreventReadOnlyEdit(queryParam: "applicationId")]
    public async Task<IActionResult> UpdateTaskStatus(Guid applicationId, Guid taskId, [FromBody] UpdateTaskStatusDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            bool updated = await _taskStatusService.UpdateTaskAndStageStatus(applicationId, taskId, request.Status);
            if (!updated)
            {
                return BadRequest("Unable to update task or stage status. Please try again.");
            }

            _context.Commit();
            return NoContent();
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
    [CheckApplicationId(queryParam: "applicationId")]
    [PreventReadOnlyEdit(queryParam: "applicationId")]
    public async Task<IActionResult> SubmitQuestionAnswer(Guid applicationId, Guid taskId, Guid questionId, [FromBody] QuestionAnswerSubmissionDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Request body cannot be null.");
            }

            ValidationResponse? validationResult = await _applicationAnswersService.ValidateQuestionAnswers(questionId, request.Answer);
            if (validationResult == null)
            {
                return BadRequest("We could not check your answer. Please try again.");
            }

            if (validationResult.Errors != null && validationResult.Errors.Any())
            {
                return BadRequest(validationResult);
            }

            bool isSuccessful = await _applicationAnswersService.SubmitAnswerAndUpdateStatus(applicationId, taskId, questionId, request.Answer);
            if (!isSuccessful)
            {
                return BadRequest("Failed to save the question answer. Please check your input and try again.");
            }

            _context.Commit();
            return NoContent();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while upserting an answer for QuestionId: {QuestionId} in TaskId: {TaskId} of ApplicationId: {ApplicationId}.", questionId, taskId, applicationId);
            throw new Exception("An error occurred while saving the answer. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves all question answers for a specific task in an application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="taskId">The ID of the task.</param>
    [HttpGet("{applicationId}/tasks/{taskId}/questions/answers")]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<ActionResult<List<TaskReviewGroupDto>>> GetTaskAnswerReview(Guid applicationId, Guid taskId)
    {
        try
        {
            var reviewAnswers = await _applicationAnswersService.GetTaskAnswerReview(applicationId, taskId);
            if (reviewAnswers != null && !reviewAnswers.Any())
            {
                return NotFound("No question answers found for the specified task and application.");
            }

            return Ok(reviewAnswers);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving question answers for TaskId: {TaskId} and ApplicationId: {ApplicationId}.", taskId, applicationId);
            throw new Exception("An error occurred while fetching the question answers. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves the answer for a specific question in an application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <param name="questionId">The ID of the question.</param>
    [HttpGet("{applicationId}/questions/{questionId}/answer")]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<ActionResult<QuestionAnswerDto>> GetQuestionAnswer(Guid applicationId, Guid questionId)
    {
        try
        {
            QuestionAnswerDto? answer = await _context.ApplicationAnswersRepository.GetQuestionAnswer(applicationId, questionId);
            if (answer == null)
            {
                return NotFound("No answer found for the specified question and application.");
            }

            return Ok(answer);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving the answer for QuestionId: {QuestionId} and ApplicationId: {ApplicationId}.", questionId, applicationId);
            throw new Exception("An error occurred while fetching the question answer. Please try again later.");
        }
    }

    /// <summary>
    /// Retrieves all answers associated with a specific application.
    /// </summary>
    /// <param name="applicationId">The ID of the application.</param>
    /// <returns>A list of task review section DTOs.</returns>
    [HttpGet("{applicationId}/tasks/answers")]
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<ActionResult<List<TaskReviewSectionDto>>> GetAllApplicationAnswersByAppId(Guid applicationId)
    {
        try
        {
            var applicationAnswers = await _applicationAnswersService.GetAllApplicationAnswerReview(applicationId);
            if (applicationAnswers != null && !applicationAnswers.Any())
            {
                return NotFound("No answers found for the specified application.");
            }

            return Ok(applicationAnswers);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving answers for ApplicationId: {ApplicationId}.", applicationId);
            throw new Exception("An error occurred while fetching the application answers. Please try again later.");
        }
    }

    /// <summary>
    /// Submits the specified application.
    /// </summary>
    /// <param name="applicationId">The application ID.</param>
    [HttpPost("{applicationId}/submit")]
    [CheckApplicationId(queryParam: "applicationId")]
    [PreventReadOnlyEdit(queryParam: "applicationId")]
    public async Task<ActionResult<ApplicationDetailsDto>> SubmitApplication(Guid applicationId)
    {
        try
        {
            ApplicationDetailsDto? application = await _applicationService.CheckAndSubmitApplication(applicationId);
            if (application == null)
            {
                return BadRequest("Unable to submit application. Required stages may be incomplete.");
            }

            _context.Commit();
            return Ok(application);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while submitting application {ApplicationId}.", applicationId);
            throw new Exception("An error occurred while submitting the application. Please try again later.");
        }
    }
}