using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories;

public interface IApplicationRepository
{
    Task<Application> CreateApplication();
}