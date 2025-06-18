using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;
    public IUserInformationService _userInformationService;

    public ApplicationRepository(IDbConnection connection, IDbTransaction transaction, IUserInformationService userInformationService)
    {
        _connection = connection;
        _transaction = transaction;
        _userInformationService = userInformationService;
    }

    public async Task<Application?> CreateApplication()
    {
        string oid = _userInformationService.GetCurrentUserObjectId();
        string displayName = _userInformationService.GetCurrentUserDisplayName();
        string upn = _userInformationService.GetCurrentUserUpn();

        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[Application] (
                    CreatedByUpn,
                    ModifiedByUpn
                ) 
                OUTPUT INSERTED.* 
                VALUES (
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";

            return await _connection.QuerySingleAsync<Application>(query, new
            {
                CreatedByUpn = upn, // TODO: replace once auth gets added
                ModifiedByUpn = upn // TODO: replace once auth gets added
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating a new application");
            return null;
        }
    }
}
