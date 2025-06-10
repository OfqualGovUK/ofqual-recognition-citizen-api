using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class QuestionTestDataBuilder
{
    public static async Task<QuestionType> CreateTestQuestionType(UnitOfWork unitOfWork)
    {
        var questionType = new QuestionType
        {
            QuestionTypeId = Guid.NewGuid(),
            QuestionTypeName = "TextBox",
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[QuestionType]
            (QuestionTypeId, QuestionTypeName, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionTypeId, @QuestionTypeName, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            questionType,
            unitOfWork.Transaction);

        return questionType;
    }

    public static async Task<Question> CreateTestQuestion(UnitOfWork unitOfWork, Guid taskId, Guid questionTypeId, int order, string url, string content)
    {
        var question = new Question
        {
            QuestionId = Guid.NewGuid(),
            TaskId = taskId,
            QuestionOrderNumber = order,
            QuestionTypeId = questionTypeId,
            QuestionContent = content,
            QuestionNameUrl = url,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            CreatedByUpn = "test@ofqual.gov.uk"
        };

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Question]
            (QuestionId, TaskId, OrderNumber, QuestionTypeId, QuestionContent, QuestionNameUrl, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionId, @TaskId, @QuestionOrderNumber, @QuestionTypeId, @QuestionContent, @QuestionNameUrl, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            question,
            unitOfWork.Transaction);

        return question;
    }
}