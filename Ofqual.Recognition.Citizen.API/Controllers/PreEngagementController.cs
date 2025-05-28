using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.PreEngagement;
using Ofqual.Recognition.Citizen.API.Infrastructure;
using Serilog;
using static System.Collections.Specialized.BitVector32;

namespace Ofqual.Recognition.Citizen.API.Controllers;

/// <summary>
/// Controller for recognition citizen Pre-Engagement tasks.
/// </summary>
[ApiController]
[Route("pre-engagement")]
public class PreEngagementController : Controller
{

    private readonly IUnitOfWork _context;
    //private readonly IMemoryCache _memoryCache;

    public PreEngagementController(IUnitOfWork context /*IMemoryCache memoryCache*/)
    {
        _context = context;
        //_memoryCache = memoryCache;
    }

    /// <summary>
    /// Retrieves sections with Pre-Engagement tasks and their statuses for a given application.
    /// </summary>
    /// <returns>A list of sections containing Pre-Engagement tasks.</returns>
    [HttpGet("tasks")]
    public async Task<ActionResult<List<PreEngagement>>> GetPreEngagementTasks()
    {
        try
        {
            var preEngagementTasks = await _context.TaskRepository.GetPreEngagementTasks();

            if (preEngagementTasks == null || !preEngagementTasks.Any())
            {
                return BadRequest("No Pre-Engagement tasks found for the specified application.");
            }

            return Ok(preEngagementTasks);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while retrieving the Pre-Engagement tasks.");
            throw new Exception("An error occurred while fetching the Pre-Engagement tasks. Please try again later.");
        }
    }
}