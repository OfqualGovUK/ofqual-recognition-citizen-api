﻿using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Controller for recognition citizen Pre-Engagement tasks.
/// </summary>
[ApiController]
[Route("pre-engagement")]
public class PreEngagementController : Controller
{
    private readonly IUnitOfWork _context;
    public readonly IApplicationAnswersService _applicationAnswersService;

    public PreEngagementController(IUnitOfWork context, IApplicationAnswersService applicationAnswersService)
    {
        _context = context;
        _applicationAnswersService = applicationAnswersService;
    }

    /// <summary>
    /// Retrieves the first Pre-Engagement question to begin the user flow.
    /// </summary>
    /// <returns>The first Pre-Engagement question with navigation details.</returns>
    [HttpGet("first-question")]
    public async Task<ActionResult<StageQuestionDto>> GetFirstPreEngagementQuestion()
    {
        try
        {
            StageQuestionDto? firstQuestion = await _context.StageRepository.GetFirstQuestionByStage(Stage.PreEngagement);
            if (firstQuestion == null)
            {
                return NotFound("No Pre-Engagement question found.");
            }

            return Ok(firstQuestion);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving the first Pre-Engagement question.");
            throw new Exception("An error occurred while fetching the first Pre-Engagement question. Please try again later.");
        }
    }

    /// <summary>
    /// Returns pre-engagement question content and type based on URL.
    /// </summary>
    /// <param name="taskNameUrl">URL-formatted task name.</param>
    /// <param name="questionNameUrl">Question name from the URL.</param>
    /// <returns>The pre-engagement question with its content and type.</returns>
    [HttpGet("{taskNameUrl}/{questionNameUrl}")]
    public async Task<ActionResult<QuestionDetailsDto?>> GetPreEngagementQuestions(string taskNameUrl, string questionNameUrl)
    {
        try
        {
            StageQuestionDetails? question = await _context.StageRepository.GetStageQuestionByTaskAndQuestionUrl(Stage.PreEngagement, taskNameUrl, questionNameUrl);
            if (question == null)
            {
                return BadRequest($"No pre-engagement question found for URL: {taskNameUrl}/{questionNameUrl}");
            }

            QuestionDetailsDto questionDto = QuestionMapper.ToDto(question);

            return Ok(questionDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred whilst retrieving pre-engagement question for URL: {TaskNameUrl}/{QuestionNameUrl}", taskNameUrl, questionNameUrl);
            throw new Exception("An error occurred while fetching the pre-engagement question. Please try again later.");
        }
    }

    /// <summary>
    /// Validates pre-engagment task without submitting data to the database.
    /// </summary>
    /// <param name="questionId">The ID of the question being answered.</param>
    /// <param name="request">The answer payload.</param>
    [HttpPost("questions/{questionId}/validate")]
    public async Task<IActionResult> ValidateAnswer(Guid questionId, [FromBody] QuestionAnswerSubmissionDto request)
    {
        try
        {
            ValidationResponse validationResult = await _applicationAnswersService.ValidateQuestionAnswers(questionId, request.Answer);
            if (!string.IsNullOrEmpty(validationResult.Message) || (validationResult.Errors != null && validationResult.Errors.Any()))
            {
                return BadRequest(validationResult);
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Something went wrong while trying to validate your answer for QuestionId: {QuestionId}.", questionId);
            throw new Exception("An unexpected error occurred while validating the answer. Please try again shortly.");
        }
    }
}