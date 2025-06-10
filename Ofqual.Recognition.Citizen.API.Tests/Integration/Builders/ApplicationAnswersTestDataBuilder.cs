using Ofqual.Recognition.Citizen.API.Infrastructure;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Dapper;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class ApplicationAnswersTestDataBuilder
{
    public static async Task<ApplicationAnswer?> GetInsertedApplicationAnswer(UnitOfWork unitOfWork, Guid applicationId, Guid questionId)
    {
        const string sql = @"
            SELECT
                ApplicationAnswersId,
                ApplicationId,
                QuestionId,
                Answer
            FROM [recognitionCitizen].[ApplicationAnswers]
            WHERE ApplicationId = @applicationId AND QuestionId = @questionId;";
        
        var answer = await unitOfWork.Connection.QuerySingleOrDefaultAsync<ApplicationAnswer>(
            sql,
            new { applicationId, questionId },
            unitOfWork.Transaction);

        return answer;
    }
}