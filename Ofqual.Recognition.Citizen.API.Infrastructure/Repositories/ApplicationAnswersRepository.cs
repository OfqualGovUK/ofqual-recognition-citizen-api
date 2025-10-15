using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class ApplicationAnswersRepository : IApplicationAnswersRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public ApplicationAnswersRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IEnumerable<SectionTaskQuestionAnswer>?> GetAllApplicationAnswers(Guid applicationId)
    {
        try
        {
            const string query = @"
                SELECT
                    @applicationId AS ApplicationId,
                    Q.QuestionId,
                    AA.Answer,
                    Q.TaskId,
                    Q.QuestionContent,
                    Q.QuestionNameUrl,
                    T.TaskName,
                    T.TaskNameUrl,
                    T.OrderNumber AS TaskOrderNumber,
                    T.ReviewFlag,
                    S.SectionId,
                    S.SectionName,
                    S.OrderNumber AS SectionOrderNumber
                FROM [recognitionCitizen].[Question] Q
                INNER JOIN [recognitionCitizen].[Task] T ON Q.TaskId = T.TaskId
                INNER JOIN [recognitionCitizen].[Section] S ON T.SectionId = S.SectionId
                LEFT JOIN [recognitionCitizen].[ApplicationAnswers] AA
                    ON AA.QuestionId = Q.QuestionId AND AA.ApplicationId = @applicationId
                ORDER BY S.OrderNumber, T.OrderNumber, Q.OrderNumber;";

            return await _connection.QueryAsync<SectionTaskQuestionAnswer>(query, new
            {
                applicationId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to retrieve all task question answers for ApplicationId: {ApplicationId}", applicationId);
            return null;
        }
    }

    public async Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer, string upn)
    {
        try
        {
            const string query = @"
                MERGE [recognitionCitizen].[ApplicationAnswers] AS target
                USING (SELECT @ApplicationId AS ApplicationId, @QuestionId AS QuestionId) AS source
                    ON target.ApplicationId = source.ApplicationId AND target.QuestionId = source.QuestionId
                WHEN MATCHED THEN
                    UPDATE SET
                        Answer = @Answer,
                        ModifiedByUpn = @ModifiedByUpn,
                        ModifiedDate = GETDATE()
                WHEN NOT MATCHED THEN
                    INSERT (
                        ApplicationId, 
                        QuestionId,
                        Answer, 
                        CreatedByUpn, 
                        ModifiedByUpn
                    )
                    VALUES (
                        @ApplicationId, 
                        @QuestionId, 
                        @Answer, 
                        @CreatedByUpn, 
                        @ModifiedByUpn
                    );";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                applicationId,
                questionId,
                answer,
                CreatedByUpn = upn,
                ModifiedByUpn = upn
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error upserting application answer. ApplicationId: {ApplicationId}, QuestionId: {QuestionId}, Answer: {Answer}", applicationId, questionId, answer);
            return false;
        }
    }

    public async Task<IEnumerable<SectionTaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId)
    {
        try
        {
            const string query = @"
                SELECT
                    S.SectionId,
                    S.SectionName,
                    S.OrderNumber AS SectionOrderNumber,
                    T.TaskId,
                    T.TaskName,
                    T.TaskNameUrl,
                    T.OrderNumber AS TaskOrderNumber,
                    Q.QuestionId,
                    Q.QuestionContent,
                    Q.QuestionNameUrl,
                    AA.Answer,
                    AA.ApplicationId
                FROM [recognitionCitizen].[Task] T
                INNER JOIN [recognitionCitizen].[Section] S ON T.SectionId = S.SectionId
                INNER JOIN [recognitionCitizen].[Question] Q ON Q.TaskId = T.TaskId
                LEFT JOIN [recognitionCitizen].[ApplicationAnswers] AA 
                    ON AA.QuestionId = Q.QuestionId AND AA.ApplicationId = @ApplicationId
                WHERE T.TaskId = @TaskId
                ORDER BY S.OrderNumber, T.OrderNumber, Q.OrderNumber";

            return await _connection.QueryAsync<SectionTaskQuestionAnswer>(query, new
            {
                ApplicationId = applicationId,
                TaskId = taskId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch question answers for TaskId: {TaskId}, ApplicationId: {ApplicationId}", taskId, applicationId);
            return Enumerable.Empty<SectionTaskQuestionAnswer>();
        }
    }

    public async Task<QuestionAnswerDto?> GetQuestionAnswer(Guid applicationId, Guid questionId)
    {
        try
        {
            const string query = @"
                SELECT
                    q.QuestionId,
                    a.Answer
                FROM [recognitionCitizen].[Question] q
                LEFT JOIN [recognitionCitizen].[ApplicationAnswers] a
                    ON a.QuestionId = q.QuestionId AND a.ApplicationId = @ApplicationId
                WHERE q.QuestionId = @QuestionId";

            return await _connection.QuerySingleOrDefaultAsync<QuestionAnswerDto>(query, new
            {
                ApplicationId = applicationId,
                QuestionId = questionId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch answer for QuestionId: {QuestionId}, ApplicationId: {ApplicationId}", questionId, applicationId);
            return null;
        }
    }

    public async Task<bool> CheckIfQuestionAnswerExists(Guid questionId, string questionItemName, string questionItemAnswer, Guid? applicationId)
    {
        try
        {
            const string query = @"
               SELECT CASE 
                    WHEN EXISTS (
                        SELECT 1
                        FROM recognitionCitizen.ApplicationAnswers AS AA
                        JOIN recognitionCitizen.Application AS A ON AA.ApplicationId = A.ApplicationId
                        WHERE @QuestionId = AA.QuestionId
                            JSON_VALUE(AA.Answer, CONCAT('$.', @questionItemName)) COLLATE SQL_Latin1_General_CP1_CI_AS = @questionItemAnswer COLLATE SQL_Latin1_General_CP1_CI_AS
                            AND (
                                @applicationId IS NULL
                                OR @applicationId <> A.ApplicationId
                            )
                            AND (
                                A.OrganisationId IS NULL
                                OR NOT EXISTS (
                                    SELECT 1
                                    FROM recognitionCitizen.Application AS A2
                                    WHERE A2.OrganisationId = A.OrganisationId
                                    AND A2.ApplicationId = A.ApplicationId
                                )
                            )
                    ) THEN 1 ELSE 0
                END;";

            return await _connection.QuerySingleAsync<bool>(
                query,
                new
                {
                    questionId,
                    questionItemName,
                    questionItemAnswer,
                    applicationId
                },
                _transaction
            );
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error checking if answer exists for QuestionId: {QuestionId} and Key: {Key}", questionId, questionItemName);
            return false;
        }
    }
}
