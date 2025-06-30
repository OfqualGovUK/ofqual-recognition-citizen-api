namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public class IGovUkNotifyService
{
    Task<bool> SendEmail(string userEmailAddress);
}
