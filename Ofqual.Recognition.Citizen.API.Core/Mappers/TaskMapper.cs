using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

/// <summary>
/// Provides mapping functions for task-related models
/// </summary>
public static class TaskMapper
{
    /// <summary>
    /// Maps a <see cref="TaskItemStatusSection"/> model to a <see cref="TaskItemStatusSectionDto"/>.
    /// </summary>
    /// <param name="section">The <see cref="TaskItemStatusSection"/> model to be mapped.</param>
    /// <returns>The mapped <see cref="TaskItemStatusSectionDto"/>.</returns>
    public static TaskItemStatusSectionDto MapToSectionWithTasks(TaskItemStatusSection section)
    {
        return new TaskItemStatusSectionDto
        {
            SectionId = section.SectionId,
            SectionName = section.SectionName,
            Tasks = new List<TaskItemStatusDto>
                {
                    new TaskItemStatusDto
                    {
                        TaskId = section.TaskId,
                        TaskName = section.TaskName,
                        Status = section.Status
                    }
                }
        };
    }

    /// <summary>
    /// Maps a collection of <see cref="TaskItemStatusSection"/> models to a list of <see cref="TaskItemStatusSectionDto"/>,
    /// ensuring sections are ordered first, followed by tasks in their respective order.
    /// </summary>
    /// <param name="sections">The collection of <see cref="TaskItemStatusSection"/> models to be mapped.</param>
    /// <returns>A list of mapped <see cref="TaskItemStatusSectionDto"/> objects.</returns>
    public static List<TaskItemStatusSectionDto> MapToSectionsWithTasks(IEnumerable<TaskItemStatusSection> sections)
    {
        return sections
            .GroupBy(ts => new { ts.SectionId, ts.SectionName, ts.SectionOrderNumber })
            .OrderBy(g => g.Key.SectionOrderNumber)
            .Select(g => new TaskItemStatusSectionDto
            {
                SectionId = g.Key.SectionId,
                SectionName = g.Key.SectionName,
                Tasks = g
                    .OrderBy(ts => ts.TaskOrderNumber)
                    .Select(ts => new TaskItemStatusDto
                    {
                        TaskId = ts.TaskId,
                        TaskName = ts.TaskName,
                        Status = ts.Status
                    })
                    .ToList()
            })
            .ToList();
    }
}
