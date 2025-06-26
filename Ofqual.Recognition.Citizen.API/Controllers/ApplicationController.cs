using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Ofqual.Recognition.Citizen.API.Attributes;

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
    private readonly IFeatureFlagService _featureFlagService;
    private readonly IApplicationAnswersService _applicationAnswersService;
    private readonly IUserInformationService _userInformationService;

    /// <summary>
    /// Initialises a new instance of <see cref="ApplicationController"/>.
    /// </summary>
    public ApplicationController(IUnitOfWork context, ITaskStatusService taskStatusService, IApplicationAnswersService applicationAnswersService, IStageService stageService, IUserInformationService userInformationService, IFeatureFlagService featureFlagService)
    {
        _context = context;
        _taskStatusService = taskStatusService;
        _applicationAnswersService = applicationAnswersService;
        _stageService = stageService;
        _userInformationService = userInformationService;
        _featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Creates a new application with initial task statuses.
    /// </summary>
    /// <returns>The created application.</returns>
    [HttpPost]
    public async Task<ActionResult<ApplicationDetailsDto>> CreateApplication([FromBody] IEnumerable<PreEngagementAnswerDto>? PreEngagementAnswers)
    {
        try
        {
            Application? application;
            ApplicationDetailsDto applicationDetailsDto;

            string oid = _userInformationService.GetCurrentUserObjectId();
            string displayName = _userInformationService.GetCurrentUserDisplayName();
            string upn = _userInformationService.GetCurrentUserUpn();

            if (_featureFlagService.IsFeatureEnabled("CheckUser"))
            {
                application = await _context.ApplicationRepository.GetLatestApplication(oid);
                if (application != null)
                {
                    applicationDetailsDto = ApplicationMapper.ToDto(application);

                    _context.Commit();
                    return Ok(applicationDetailsDto);
                }
            }

            application = await _context.ApplicationRepository.CreateApplication(oid, displayName, upn);
            if (application == null)
            {
                return BadRequest("Application could not be created.");
            }

            bool taskStatusesCreated = await _taskStatusService.DetermineAndCreateTaskStatuses(application.ApplicationId, PreEngagementAnswers);
            if (!taskStatusesCreated)
            {
                return BadRequest("Failed to create task statuses for the new application.");
            }

            if (PreEngagementAnswers != null && PreEngagementAnswers.Any())
            {
                bool isPreEngagementAnswersInserted = await _applicationAnswersService.SavePreEngagementAnswers(application.ApplicationId, PreEngagementAnswers);
                if (!isPreEngagementAnswersInserted)
                {
                    return BadRequest("Failed to insert pre-engagement answers for the new application.");
                }
            }

            bool stageStatusUpdated = await _stageService.EvaluateAndUpsertStageStatus(application.ApplicationId, Stage.PreEngagement);
            if (!stageStatusUpdated)
            {
                return BadRequest("Unable to determine or save the stage status for the application.");
            }

            applicationDetailsDto = ApplicationMapper.ToDto(application);

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
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<ActionResult<List<TaskItemStatusSectionDto>>> GetApplicationTasks(Guid applicationId)
    {
        try
        {
            var taskStatuses = await _context.TaskRepository.GetTaskStatusesByApplicationId(applicationId);
            if (taskStatuses == null || !taskStatuses.Any())
            {
                return BadRequest("No tasks found for the specified application.");
            }

            var taskItemStatusSectionList = TaskMapper.ToDto(taskStatuses);

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
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<IActionResult> UpdateTaskStatus(Guid applicationId, Guid taskId, [FromBody] UpdateTaskStatusDto request)
    {
        try
        {
            bool isStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, request.Status);
            if (!isStatusUpdated)
            {
                return BadRequest("Failed to update task status. Either the task does not exist or belongs to a different application.");
            }

            if (request.Status == TaskStatusEnum.Completed)
            {
                bool stageStatusUpdated = await _stageService.EvaluateAndUpsertStageStatus(applicationId, Stage.PreEngagement);
                if (!stageStatusUpdated)
                {
                    return BadRequest("Unable to determine or save the stage status for the application.");
                }
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
    [CheckApplicationId(queryParam: "applicationId")]
    public async Task<IActionResult> SubmitQuestionAnswer(Guid applicationId, Guid taskId, Guid questionId, [FromBody] QuestionAnswerSubmissionDto request)
    {
        try
        {
            ValidationResponse? validationResult = await _applicationAnswersService.ValidateQuestionAnswers(questionId, request.Answer);
            if (validationResult == null)
            {
                return BadRequest("We could not check your answer. Please try again.");
            }

            if (validationResult.Errors != null && validationResult.Errors.Any())
            {
                return BadRequest(validationResult);
            }

            bool isAnswerUpserted = await _context.ApplicationAnswersRepository.UpsertQuestionAnswer(applicationId, questionId, request.Answer);
            if (!isAnswerUpserted)
            {
                return BadRequest("Failed to save the question answer. Please check your input and try again.");
            }

            bool isStatusUpdated = await _context.TaskRepository.UpdateTaskStatus(applicationId, taskId, TaskStatusEnum.InProgress);
            if (!isStatusUpdated)
            {
                return BadRequest("Failed to update task status. Either the task does not exist or belongs to a different application.");
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
    public async Task<ActionResult<List<QuestionAnswerSectionDto>>> GetTaskAnswerReview(Guid applicationId, Guid taskId)
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

    [HttpPost("{applicationId}/complete")]
    public async Task<IActionResult> CompleteApplication(Guid applicationId)
    {
        return Ok();
    }
}