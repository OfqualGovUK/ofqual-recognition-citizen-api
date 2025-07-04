namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IGovUkNotifyService
{
    public Task<bool> SendEmailAccountCreation();
    public Task<bool> SendEmailApplicationSubmitted();
    public Task<bool> SendEmailInformationFromPreEngagement();
}
