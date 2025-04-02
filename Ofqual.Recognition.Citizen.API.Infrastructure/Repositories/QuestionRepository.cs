using System.Data;
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Serilog;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class QuestionRepository : IQuestionRepository
{
    private readonly IDbTransaction _dbTransaction;

    public QuestionRepository(IDbTransaction dbTransaction)
    {
        _dbTransaction = dbTransaction;
    }

    public async Task<QuestionDto?> GetQuestion(string questionURL)
    {
        try
        {
            var query = @"
                SELECT
                    Q.QuestionId,
                    Q.QuestionContent,
                    QT.QuestionTypeName
                FROM recognitionCitizen.Question Q
                INNER JOIN recognitionCitizen.QuestionType QT ON Q.QuestionTypeId = QT.QuestionTypeId
                WHERE Q.QuestionURL = @questionURL";
                
            var result = await _dbTransaction.Connection!
                .QueryFirstOrDefaultAsync<QuestionDto>(query, new { questionURL }, _dbTransaction);
            
            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving question for QuestionURL: {questionURL}", questionURL);
            return null;
        }
    }
}