using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

/// <summary>
/// Provides mapping functions for task-related models
/// </summary>
public static class TaskMapper
{
    ///<summary>
    /// Maps a collection of <see cref="TaskItemStatusSection"/> to a list of <see cref="TaskItemStatusSectionDto"/>,
    /// ordered by section and then by task.
    /// </summary>
    /// <param name="sections">The source collection to map.</param>
    /// <returns>A list of <see cref="TaskItemStatusSectionDto"/>.</returns>
    public static List<TaskItemStatusSectionDto> ToDto(IEnumerable<TaskItemStatusSection> sections)
    {
        return sections
            .GroupBy(ts => new { ts.SectionId, ts.SectionName, ts.SectionOrderNumber })
            .OrderBy(g => g.Key.SectionOrderNumber)
            .Select(g => new TaskItemStatusSectionDto
            {
                SectionName = g.Key.SectionName,
                Tasks = g
                    .OrderBy(ts => ts.TaskOrderNumber)
                    .Select(ts => new TaskItemStatusDto
                    {
                        TaskId = ts.TaskId,
                        TaskName = ts.TaskName,
                        Status = ts.Status,
                        FirstQuestionUrl = $"{ts.TaskNameUrl}/{ts.QuestionNameUrl}"
                    })
                    .ToList()
            })
            .ToList();
    }

    /// <summary>
    /// Maps a <see cref="TaskItem"/> to a <see cref="TaskItemDto"/>.
    /// </summary>
    /// <param name="taskItem">The source task entity to map.</param>
    /// <returns>A mapped <see cref="TaskItemDto"/> instance.</returns>
    public static TaskItemDto ToDto(TaskItem taskItem)
    {
        return new TaskItemDto
        {
            TaskId = taskItem.TaskId,
            TaskName = taskItem.TaskName,
            TaskNameUrl = taskItem.TaskNameUrl,
            TaskOrderNumber = taskItem.TaskOrderNumber,
            SectionId = taskItem.SectionId
        };
    }
}