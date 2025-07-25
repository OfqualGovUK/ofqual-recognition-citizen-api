using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class QuestionMapper
{
    /// <summary>
    /// Maps a <see cref="QuestionDetails"/> data model to a <see cref="QuestionDetailsDto"/>.
    /// </summary>
    public static QuestionDetailsDto ToDto(QuestionDetails taskQuestion)
    {
        return new QuestionDetailsDto
        {
            QuestionId = taskQuestion.QuestionId,
            TaskId = taskQuestion.TaskId,
            QuestionTypeName = taskQuestion.QuestionTypeName,
            QuestionContent = taskQuestion.QuestionContent,
            CurrentQuestionUrl = $"{taskQuestion.TaskNameUrl}/{taskQuestion.CurrentQuestionNameUrl}",
            PreviousQuestionUrl = taskQuestion.PreviousQuestionNameUrl != null
                ? $"{taskQuestion.TaskNameUrl}/{taskQuestion.PreviousQuestionNameUrl}"
                : null,
            NextQuestionUrl = taskQuestion.NextQuestionNameUrl != null
                ? $"{taskQuestion.TaskNameUrl}/{taskQuestion.NextQuestionNameUrl}"
                : null
        };
    }

    /// <summary>
    /// Maps a <see cref="StageQuestionDetails"/> data model to a <see cref="QuestionDetailsDto"/>.
    /// </summary>
    public static QuestionDetailsDto ToDto(StageQuestionDetails stageQuestionDetails)
    {
        return new QuestionDetailsDto
        {
            QuestionId = stageQuestionDetails.QuestionId,
            TaskId = stageQuestionDetails.TaskId,
            QuestionTypeName = stageQuestionDetails.QuestionTypeName,
            QuestionContent = stageQuestionDetails.QuestionContent,
            CurrentQuestionUrl = $"{stageQuestionDetails.CurrentTaskNameUrl}/{stageQuestionDetails.CurrentQuestionNameUrl}",
            PreviousQuestionUrl = stageQuestionDetails.PreviousQuestionNameUrl != null
                ? $"{stageQuestionDetails.PreviousTaskNameUrl}/{stageQuestionDetails.PreviousQuestionNameUrl}"
                : null,
            NextQuestionUrl = stageQuestionDetails.NextQuestionNameUrl != null
                ? $"{stageQuestionDetails.NextTaskNameUrl}/{stageQuestionDetails.NextQuestionNameUrl}"
                : null
        };
    }
}