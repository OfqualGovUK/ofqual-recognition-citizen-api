using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class QuestionTestDataBuilder
{
    public static async Task<QuestionTypeItem> CreateTestQuestionType(UnitOfWork unitOfWork, QuestionTypeItem questionType)
    {
        questionType.QuestionType ??= QuestionTypeEnum.TextArea;

        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[QuestionType]
            (QuestionTypeId, QuestionTypeName, QuestionType, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionTypeId, @QuestionTypeName, @QuestionType, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            questionType,
            unitOfWork.Transaction);

        return questionType;
    }

    public static async Task<Question> CreateTestQuestion(UnitOfWork unitOfWork, Question question)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Question]
            (QuestionId, TaskId, OrderNumber, QuestionTypeId, QuestionContent, QuestionNameUrl, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionId, @TaskId, @QuestionOrderNumber, @QuestionTypeId, @QuestionContent, @QuestionNameUrl, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            question,
            unitOfWork.Transaction);

        return question;
    }
}