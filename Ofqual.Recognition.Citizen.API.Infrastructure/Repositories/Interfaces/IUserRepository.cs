using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Repositories.Interfaces;

public interface IUserRepository
{
    public Task<User?> CreateUser(string oid, string displayName, string emailAddress);
}