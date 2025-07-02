using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class TaskMapper
{
    /// <summary>
    /// Maps a collection of <see cref="TaskItemStatusSection"/> to a list of <see cref="TaskItemStatusSectionDto"/>.
    /// </summary>
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
                        HintText = ts.HintText,
                        Status = ts.Status,
                        FirstQuestionUrl = $"{ts.TaskNameUrl}/{ts.QuestionNameUrl}"
                    })
                    .ToList()
            })
            .ToList();
    }

    /// <summary>
    /// Maps a <see cref="TaskItem"/> data model to a <see cref="TaskItemDto"/>.
    /// </summary>
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