namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IGovUkNotifyService
{
    public Task<bool> SendEmail(string userEmailAddress);
}
