using Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using System.Data;
using Serilog;
using Dapper;
using Ofqual.Recognition.Citizen.API.Core.Models.ApplicationQueryParameter.cs;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public ApplicationRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<Application?> CreateApplication()
    {
        try
        {
            const string query = @"
                INSERT INTO [recognitionCitizen].[Application] (
                    CreatedByUpn,
                    ModifiedByUpn
                ) OUTPUT INSERTED.* VALUES (
                    @CreatedByUpn,
                    @ModifiedByUpn
                )";

            return await _connection.QuerySingleAsync<Application>(query, new
            {
                CreatedByUpn = "USER", // TODO: replace once auth gets added
                ModifiedByUpn = "USER" // TODO: replace once auth gets added
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating a new application");
            return null!;
        }
    }

    public async Task<Application?> GetApplications(ApplicationQueryParameter searchParams = [])
    {
        try
        {
            // get list of applications from db
            // where search params have been passed, append sql query with WHERE predicates in order to filter
            const string query = @"
                
            ";

            // return list of applications
            return await _connection.QuerySingleAsync<Application>(query, new
            {
                OrganisationName = searchParams.OrganisationName,
                LegalName = searchParams.LegalName,
                Acronym = searchParams.Acronym,
                Website = searchParams.Website,
                FullName = searchParams.FullName,
                Email = searchParams.Email,
                PhoneNumber = searchParams.PhoneNumber,
                JobRole = searchParams.JobRole
            }, _transaction);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error retrieving list of applications with search parameters: {searchParams}", searchParams);
            return null!;
        }
    }
}
