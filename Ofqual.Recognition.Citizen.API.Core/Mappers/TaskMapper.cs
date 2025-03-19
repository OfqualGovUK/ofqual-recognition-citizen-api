using Ofqual.Recognition.Citizen.API.Core.Models.TaskStatuses;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

/// <summary>
/// Provides mapping functions for task-related models, ensuring structured grouping and ordering.
/// </summary>
public static class TaskMapper
{
    /// <summary>
    /// Maps a <see cref="TaskItemStatusSection"/> model to a <see cref="TaskItemStatusSectionDto"/>, 
    /// preserving section structure.
    /// </summary>
    public static TaskItemStatusSectionDto MapToSectionWithTasks(TaskItemStatusSection section)
    {
        return new TaskItemStatusSectionDto
        {
            SectionId = section.SectionId,
            SectionName = section.SectionName,
            SectionOrderNumber = section.SectionOrderNumber,
            Tasks = new List<TaskItemStatusDto>
                {
                    new TaskItemStatusDto
                    {
                        TaskId = section.TaskId,
                        TaskName = section.TaskName,
                        TaskOrderNumber = section.TaskOrderNumber,
                        TaskStatusId = section.TaskStatusId,
                        Status = section.Status
                    }
                }
        };
    }

    /// <summary>
    /// Maps a collection of <see cref="TaskItemStatusSection"/> models to a list of <see cref="TaskItemStatusSectionDto"/>,
    /// ensuring sections are ordered first, followed by tasks in their respective order.
    /// </summary>
    public static List<TaskItemStatusSectionDto> MapToSectionsWithTasks(IEnumerable<TaskItemStatusSection> sections)
    {
        return sections
            .GroupBy(ts => new { ts.SectionId, ts.SectionName, ts.SectionOrderNumber })
            .OrderBy(g => g.Key.SectionOrderNumber)
            .Select(g => new TaskItemStatusSectionDto
            {
                SectionId = g.Key.SectionId,
                SectionName = g.Key.SectionName,
                SectionOrderNumber = g.Key.SectionOrderNumber,
                Tasks = g
                    .OrderBy(ts => ts.TaskOrderNumber)
                    .Select(ts => new TaskItemStatusDto
                    {
                        TaskId = ts.TaskId,
                        TaskName = ts.TaskName,
                        TaskOrderNumber = ts.TaskOrderNumber,
                        TaskStatusId = ts.TaskStatusId,
                        Status = ts.Status
                    })
                    .ToList()
            })
            .ToList();
    }
}
