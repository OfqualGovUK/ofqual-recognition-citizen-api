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
    /// <param name="taskUrlName">URL-formatted task name.</param>
    /// <param name="questionUrlName">Question name from the URL.</param>
    /// <returns>The question with its content and type.</returns>
    [HttpGet("{taskUrlName}/{questionUrlName}")]
    public async Task<ActionResult<QuestionDto?>> GetQuestions(string taskUrlName, string questionUrlName)
    {
        try
        {
            TaskQuestion? question = await _context.QuestionRepository.GetQuestion(taskUrlName, questionUrlName);

            if (question == null)
            {
                return BadRequest($"No question found with URL: {taskUrlName}/{questionUrlName}");
            }

            QuestionDto questionDto = QuestionMapper.ToDto(question);

            return Ok(questionDto);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving question for URL: {taskUrlName}/{questionName}", taskUrlName, questionUrlName);
            throw new Exception("An error occurred while fetching the question. Please try again later.");
        }
    }
}