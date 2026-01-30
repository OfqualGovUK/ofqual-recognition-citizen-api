using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class QuestionTestDataBuilder
{
    public static async Task<Question> CreateTestQuestion(UnitOfWork unitOfWork, Question question)
    {
        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Question]
            (QuestionId, TaskId, OrderNumber, QuestionTypeKey, QuestionContent, QuestionNameUrl, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionId, @TaskId, @QuestionOrderNumber, @QuestionType, @QuestionContent, @QuestionNameUrl, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            question,
            unitOfWork.Transaction);

        return question;
    }
}