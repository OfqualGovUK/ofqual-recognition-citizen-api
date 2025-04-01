using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Handles question-related operations within a task.
/// </summary>
[ApiController]
[Route("questions")]
public class QuestionController : ControllerBase
{
    private readonly IUnitOfWork _context;

    /// <summary>
    /// Initialises a new instance of <see cref="QuestionController"/>.
    /// </summary>
    public QuestionController(IUnitOfWork context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns question content and type based on URL.
    /// </summary>
    /// <param name="taskName">URL-formatted task name.</param>
    /// <param name="questionName">Question name from the URL.</param>
    /// <returns>The question with its content and type.</returns>
    [HttpGet("{taskName}/{questionName}")]
    public async Task<ActionResult<QuestionDto?>> GetQuestions(string taskName, string questionName)
    {
        try
        {
            QuestionDto? question = await _context.QuestionRepository.GetQuestion($"{taskName}/{questionName}");

            if (question == null)
            {
                return BadRequest($"No question found with URL: {taskName}/{questionName}");
            }

            return question;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving question for URL: {taskName}/{questionName}", taskName, questionName);
            throw new Exception("An error occurred while fetching the question. Please try again later.");
        }
    }
}