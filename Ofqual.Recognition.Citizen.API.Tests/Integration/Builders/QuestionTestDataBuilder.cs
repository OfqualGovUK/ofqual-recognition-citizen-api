using Dapper;
using Ofqual.Recognition.API.Models.JSON.Questions;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;
using Ofqual.Recognition.Citizen.API.Infrastructure;

namespace Ofqual.Recognition.Citizen.Tests.Integration.Builders;

public static class QuestionTestDataBuilder
{
    public static async Task<Question> CreateTestQuestion(UnitOfWork unitOfWork, Question question)
    {
        //This statement is a quick workaround to allow us to run unit tests against the current database while the QuestionType table still exists.
        //TODO: Remove this check once the QuestionType table has been removed from the main database.
        if (unitOfWork.Connection.ExecuteScalarAsync<int>(
            @"  SELECT  COUNT(*) 
                FROM    sys.tables 
                WHERE   SCHEMA_NAME(schema_id) = N'recognitionCitizen' 
                AND     OBJECT_NAME(object_id) = N'QuestionType';"
            , null, unitOfWork.Transaction).Result > 0)

        {
            var questionTypeId = Guid.NewGuid();

            await unitOfWork.Connection.ExecuteAsync(@"
            INSERT  INTO[recognitionCitizen].[QuestionType]            
                    (QuestionTypeId, QuestionTypeName, CreatedDate, ModifiedDate, CreatedByUpn, ModifiedByUpn)
            VALUES  (@QuestionTypeId, @QuestionTypeName, @CreatedDate, @ModifiedDate, @CreatedByUpn, @CreatedByUpn); ",
            new
            { 
                questionTypeId,
                questionTypeName = question.QuestionType.ToString(),
                question.CreatedDate,
                question.ModifiedDate,
                question.CreatedByUpn

            },
            unitOfWork.Transaction);

            await unitOfWork.Connection.QueryAsync<Question>(@"
            INSERT  INTO [recognitionCitizen].[Question]
                    (   QuestionId, TaskId, OrderNumber, QuestionTypeKey, QuestionTypeId, 
                        QuestionContent, QuestionNameUrl, CreatedDate, ModifiedDate, CreatedByUpn)
            OUTPUT  INSERTED.QuestionId, 
                    INSERTED.TaskId, 
                    INSERTED.OrderNumber AS [QuestionOrderNumber],
                    INSERTED.QuestionTypeKey AS [QuestionType],
                    INSERTED.QuestionContent,
                    INSERTED.QuestionNameUrl,
                    INSERTED.CreatedDate,
                    INSERTED.ModifiedDate,
                    INSERTED.CreatedByUpn
            VALUES  (   @QuestionId, @TaskId, @OrderNumber, @QuestionTypeKey, @QuestionTypeId,
                        @QuestionContent, @QuestionNameUrl, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            new 
            { 
                question.QuestionId,
                question.TaskId,
                OrderNumber =  question.QuestionOrderNumber,
                QuestionTypeKey = question.QuestionType,                
                questionTypeId,
                question.QuestionContent,
                question.QuestionNameUrl,
                question.CreatedDate,
                question.ModifiedDate,
                question.CreatedByUpn
            },
            unitOfWork.Transaction);
            return question;
        }



        await unitOfWork.Connection.ExecuteAsync(@"
            INSERT INTO [recognitionCitizen].[Question]
            (QuestionId, TaskId, OrderNumber, QuestionTypeKey, QuestionContent, QuestionNameUrl, CreatedDate, ModifiedDate, CreatedByUpn)
            VALUES (@QuestionId, @TaskId, @QuestionOrderNumber, @QuestionType, @QuestionContent, @QuestionNameUrl, @CreatedDate, @ModifiedDate, @CreatedByUpn);",
            question,
            unitOfWork.Transaction);

        return question;
    }
}