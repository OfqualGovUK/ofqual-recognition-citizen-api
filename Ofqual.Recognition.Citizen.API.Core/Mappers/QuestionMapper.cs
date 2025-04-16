using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class QuestionMapper
{
    /// <summary>
    /// Maps a <see cref="TaskQuestion"/> data model to a <see cref="QuestionDto"/>.
    /// </summary>
    /// <param name="taskQuestion">The data model to map.</param>
    /// <returns>A mapped <see cref="QuestionDto"/>.</returns>
    public static QuestionDto ToDto(TaskQuestion taskQuestion)
    {
        return new QuestionDto
        {
            QuestionId = taskQuestion.QuestionId,
            TaskId = taskQuestion.TaskId,
            QuestionTypeName = taskQuestion.QuestionTypeName,
            QuestionContent = taskQuestion.QuestionContent,
            CurrentQuestionUrl = $"{taskQuestion.TaskNameUrl}/{taskQuestion.CurrentQuestionNameUrl}",
            PreviousQuestionUrl = taskQuestion.PreviousQuestionNameUrl != null
                ? $"{taskQuestion.TaskNameUrl}/{taskQuestion.PreviousQuestionNameUrl}"
                : null
        };
    }
}