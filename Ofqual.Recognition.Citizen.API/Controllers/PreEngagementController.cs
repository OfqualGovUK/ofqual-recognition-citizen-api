using Microsoft.AspNetCore.Mvc;
using Ofqual.Recognition.Citizen.API.Core.Mappers;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;

namespace Ofqual.Recognition.Citizen.API.Controllers
{
    /// <summary>
    /// Controller for recognition citizen Pre-Engagement tasks.
    /// </summary>
    [ApiController]
    [Route("pre-engagement")]
    public class PreEngagementController : Controller
    {

        private readonly IUnitOfWork _context;

        /// <summary>
        /// Retrieves sections with Pre-Engagement tasks and their statuses for a given application.
        /// </summary>
        /// <param name="stageTaskId">The stage Task ID.</param>
        /// <returns>A list of sections containing Pre-Engagement tasks with statuses.</returns>
        [HttpGet("{stageTaskId}/tasks")]
        public async Task<ActionResult<List<TaskItemStatusSectionDto>>> GetPreEngagementTasks(int stageTaskId)
        {
            try
            {
                var taskStatuses = await _context.TaskRepository.GetPreEngagementTasksByStageTaskId(stageTaskId);

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
    }
}
