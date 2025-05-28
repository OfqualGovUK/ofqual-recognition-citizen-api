using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class QuestionMapper
{
    /// <summary>
    /// Maps a <see cref="TaskQuestion"/> data model to a <see cref="QuestionDto"/>.
    /// </summary>
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

    /// <summary>
    /// Maps a <see cref="PreEngagementQuestionDetails"/> data model to a <see cref="PreEngagementQuestionDetailsDto"/>.
    /// </summary>
    public static PreEngagementQuestionDetailsDto ToDto(PreEngagementQuestionDetails preEngagement)
    {
        return new PreEngagementQuestionDetailsDto
        {
            QuestionId = preEngagement.QuestionId,
            TaskId = preEngagement.TaskId,
            QuestionTypeName = preEngagement.QuestionTypeName,
            QuestionContent = preEngagement.QuestionContent,
            CurrentQuestionUrl = $"{preEngagement.CurrentTaskNameUrl}/{preEngagement.CurrentQuestionNameUrl}",
            PreviousQuestionUrl = preEngagement.PreviousQuestionNameUrl != null
                ? $"{preEngagement.PreviousTaskNameUrl}/{preEngagement.PreviousQuestionNameUrl}"
                : null,
            NextQuestionUrl = preEngagement.NextQuestionNameUrl != null
                ? $"{preEngagement.NextTaskNameUrl}/{preEngagement.NextQuestionNameUrl}"
                : null
        };
    }
}