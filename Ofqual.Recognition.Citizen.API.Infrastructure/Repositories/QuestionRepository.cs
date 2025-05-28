using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public QuestionRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<TaskQuestion?> GetQuestion(string taskNameUrl, string questionNameUrl)
    {
        try
        {
            var query = @"
                SELECT
                    Q.QuestionId,
                    Q.QuestionContent,
                    Q.TaskId,
                    Q.QuestionNameUrl AS CurrentQuestionNameUrl,
                    QT.QuestionTypeName,
                    (
                        SELECT TOP 1 prev.QuestionNameUrl
                        FROM recognitionCitizen.Question prev
                        WHERE prev.TaskId = Q.TaskId
                        AND prev.OrderNumber < Q.OrderNumber
                        ORDER BY prev.OrderNumber DESC
                    ) AS PreviousQuestionNameUrl,
                    T.TaskNameUrl
                FROM recognitionCitizen.Question Q
                INNER JOIN recognitionCitizen.QuestionType QT ON Q.QuestionTypeId = QT.QuestionTypeId
                INNER JOIN recognitionCitizen.Task T ON Q.TaskId = T.TaskId
                WHERE Q.QuestionNameUrl = @questionNameUrl AND T.TaskNameUrl = @taskNameUrl";

            return await _connection.QueryFirstOrDefaultAsync<TaskQuestion>(query, new
            {
                taskNameUrl,
                questionNameUrl
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question for TaskNameUrl: {TaskNameUrl}, QuestionNameUrl: {questionNameUrl}", taskNameUrl, questionNameUrl);
            return null;
        }
    }

    public async Task<PreEngagementQuestionDetails?> GetPreEngagementQuestion(string taskNameUrl, string questionNameUrl)
    {
        try
        {
            var query = @"
                WITH OrderedQuestions AS (
                    SELECT 
                        q.QuestionId,
                        q.QuestionContent,
                        q.TaskId,
                        q.QuestionNameUrl AS CurrentQuestionNameUrl,
                        qt.QuestionTypeName,
                        t.TaskNameUrl AS CurrentTaskNameUrl,
                        LEAD(q.QuestionNameUrl) OVER (ORDER BY s.OrderNumber, t.OrderNumber, q.OrderNumber) AS NextQuestionNameUrl,
                        LEAD(t.TaskNameUrl) OVER (ORDER BY s.OrderNumber, t.OrderNumber, q.OrderNumber) AS NextTaskNameUrl,
                        LAG(q.QuestionNameUrl) OVER (ORDER BY s.OrderNumber, t.OrderNumber, q.OrderNumber) AS PreviousQuestionNameUrl,
                        LAG(t.TaskNameUrl) OVER (ORDER BY s.OrderNumber, t.OrderNumber, q.OrderNumber) AS PreviousTaskNameUrl
                    FROM recognitionCitizen.Question q
                    INNER JOIN recognitionCitizen.QuestionType qt ON q.QuestionTypeId = qt.QuestionTypeId
                    INNER JOIN recognitionCitizen.Task t ON q.TaskId = t.TaskId
                    INNER JOIN recognitionCitizen.Section s ON t.SectionId = s.SectionId
                    INNER JOIN recognitionCitizen.StageTask st ON st.TaskId = t.TaskId
                    INNER JOIN recognitionCitizen.Ref_V_Stage rs ON rs.KeyValueId = st.StageId
                    WHERE rs.LookUpKey = N'Pre-application Enagagement'
                )
                SELECT
                    QuestionId,
                    QuestionContent,
                    TaskId,
                    CurrentQuestionNameUrl,
                    QuestionTypeName,
                    CurrentTaskNameUrl,
                    NextQuestionNameUrl,
                    NextTaskNameUrl,
                    PreviousQuestionNameUrl,
                    PreviousTaskNameUrl
                FROM OrderedQuestions
                WHERE CurrentTaskNameUrl = @taskNameUrl AND CurrentQuestionNameUrl = @questionNameUrl;";

            return await _connection.QueryFirstOrDefaultAsync<PreEngagementQuestionDetails>(query, new
            {
                taskNameUrl,
                questionNameUrl
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving pre-engagement question for TaskNameUrl: {TaskNameUrl}, QuestionNameUrl: {QuestionNameUrl}", taskNameUrl, questionNameUrl);
            return null;
        }
    }

    public async Task<PreEngagementQuestionDto?> GetFirstPreEngagementQuestion()
    {
        try
        {
            var query = @"
                WITH OrderedQuestions AS (
                    SELECT
                        q.QuestionId,
                        q.TaskId,
                        q.QuestionNameUrl AS CurrentQuestionNameUrl,
                        t.TaskNameUrl AS CurrentTaskNameUrl,
                        ROW_NUMBER() OVER (ORDER BY s.OrderNumber, t.OrderNumber, q.OrderNumber) AS RowNum
                    FROM recognitionCitizen.Question q
                    INNER JOIN recognitionCitizen.Task t ON q.TaskId = t.TaskId
                    INNER JOIN recognitionCitizen.Section s ON t.SectionId = s.SectionId
                    INNER JOIN recognitionCitizen.StageTask st ON st.TaskId = t.TaskId
                    INNER JOIN recognitionCitizen.Ref_V_Stage rs ON rs.KeyValueId = st.StageId
                    WHERE rs.LookUpKey = N'Pre-application Enagagement'
                )
                SELECT
                    QuestionId,
                    TaskId,
                    CurrentTaskNameUrl,
                    CurrentQuestionNameUrl
                FROM OrderedQuestions
                WHERE RowNum = 1;";

            return await _connection.QueryFirstOrDefaultAsync<PreEngagementQuestionDto>(query, transaction: _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving first pre-engagement question.");
            return null;
        }
    }

    public async Task<bool> InsertPreEngagementAnswers(Guid applicationId, IEnumerable<PreEngagementAnswerDto> answers)
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[ApplicationAnswers] (
                    ApplicationId,
                    QuestionId,
                    Answer,
                    CreatedByUpn,
                    ModifiedByUpn,
                    CreatedDate,
                    ModifiedDate
                ) VALUES (
                    @ApplicationId,
                    @QuestionId,
                    @Answer,
                    @CreatedByUpn,
                    @ModifiedByUpn,
                    @CreatedDate,
                    @ModifiedDate
                );";

            var parameters = answers.Select(answer => new
            {
                ApplicationId = applicationId,
                answer.QuestionId,
                Answer = answer.AnswerJson,
                CreatedByUpn = "USER",         // TODO: replace once auth gets added
                ModifiedByUpn = "USER",        // TODO: replace once auth gets added
                CreatedDate = answer.SubmittedDate,
                ModifiedDate = answer.SubmittedDate
            }).ToList();

            int rowsAffected = await _connection.ExecuteAsync(query, parameters, _transaction);
            return rowsAffected == parameters.Count;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error inserting application answers for ApplicationId: {ApplicationId}", applicationId);
            return false;
        }
    }

    public async Task<QuestionAnswerSubmissionResponseDto?> GetNextQuestionUrl(Guid currentQuestionId)
    {
        try
        {
            const string query = @"
                SELECT TOP 1
                    T.TaskNameUrl AS NextTaskNameUrl,
                    [next].QuestionNameUrl AS NextQuestionNameUrl
                FROM [recognitionCitizen].[Question] AS [current]
                JOIN [recognitionCitizen].[Question] AS [next]
                    ON [current].TaskId = [next].TaskId
                JOIN [recognitionCitizen].[Task] AS T
                    ON [next].TaskId = T.TaskId
                WHERE [current].QuestionId = @QuestionId
                AND [next].OrderNumber > [current].OrderNumber
                ORDER BY [next].OrderNumber ASC";

            return await _connection.QueryFirstOrDefaultAsync<QuestionAnswerSubmissionResponseDto>(query, new
            {
                QuestionId = currentQuestionId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving next question URL for QuestionId: {QuestionId}", currentQuestionId);
            return null;
        }
    }

    public async Task<bool> UpsertQuestionAnswer(Guid applicationId, Guid questionId, string answer)
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
                        ModifiedByUpn = @ModifiedByUpn
                WHEN NOT MATCHED THEN
                    INSERT (ApplicationId, QuestionId, Answer, CreatedByUpn, ModifiedByUpn)
                    VALUES (@ApplicationId, @QuestionId, @Answer, @CreatedByUpn, @ModifiedByUpn);";

            var rowsAffected = await _connection.ExecuteAsync(query, new
            {
                applicationId,
                questionId,
                answer,
                CreatedByUpn = "USER", // TODO: replace once auth gets added
                ModifiedByUpn = "USER" // TODO: replace once auth gets added
            }, _transaction);

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error upserting application answer. ApplicationId: {ApplicationId}, QuestionId: {QuestionId}, Answer: {Answer}", applicationId, questionId, answer);
            return false;
        }
    }

    public async Task<IEnumerable<TaskQuestionAnswer>> GetTaskQuestionAnswers(Guid applicationId, Guid taskId)
    {
        try
        {
            const string query = @"
                SELECT
                    t.TaskId,
                    t.TaskName,
                    t.TaskNameUrl,
                    t.OrderNumber AS TaskOrder,
                    q.QuestionId,
                    q.QuestionContent,
                    q.QuestionNameUrl,
                    a.Answer
                FROM [recognitionCitizen].[Task] t
                INNER JOIN [recognitionCitizen].[Question] q ON q.TaskId = t.TaskId
                LEFT JOIN [recognitionCitizen].[ApplicationAnswers] a
                    ON a.QuestionId = q.QuestionId AND a.ApplicationId = @ApplicationId
                WHERE t.TaskId = @TaskId
                ORDER BY t.OrderNumber, q.OrderNumber";

            return await _connection.QueryAsync<TaskQuestionAnswer>(query, new
            {
                ApplicationId = applicationId,
                TaskId = taskId
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch question answers for TaskId: {TaskId}, ApplicationId: {ApplicationId}", taskId, applicationId);
            return Enumerable.Empty<TaskQuestionAnswer>();
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
}