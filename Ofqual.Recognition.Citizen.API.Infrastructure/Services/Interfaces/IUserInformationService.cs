namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IUserInformationService
{
    public string GetCurrentUserObjectId();
    public string GetCurrentUserDisplayName();
    public string GetCurrentUserUpn();
}