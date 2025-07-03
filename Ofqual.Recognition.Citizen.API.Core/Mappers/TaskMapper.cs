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
}