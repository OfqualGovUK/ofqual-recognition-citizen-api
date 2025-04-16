using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Microsoft.AspNetCore.Mvc;
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
    /// <param name="TaskNameUrl">URL-formatted task name.</param>
    /// <param name="questionNameUrl">Question name from the URL.</param>
    /// <returns>The question with its content and type.</returns>
    [HttpGet("{TaskNameUrl}/{questionNameUrl}")]
    public async Task<ActionResult<QuestionDto?>> GetQuestions(string TaskNameUrl, string questionNameUrl)
    {
        try
        {
            TaskQuestion? question = await _context.QuestionRepository.GetQuestion(TaskNameUrl, questionNameUrl);

            if (question == null)
            {
                return BadRequest($"No question found with URL: {TaskNameUrl}/{questionNameUrl}");
            }

            QuestionDto questionDto = QuestionMapper.ToDto(question);

            return Ok(questionDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving question for URL: {TaskNameUrl}/{questionNameUrl}", TaskNameUrl, questionNameUrl);
            throw new Exception("An error occurred while fetching the question. Please try again later.");
        }
    }
}