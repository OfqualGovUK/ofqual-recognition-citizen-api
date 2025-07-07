using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Notify.Client;
using Serilog;
using Polly;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class GovUkNotifyService : IGovUkNotifyService
{
    private readonly GovUkNotifyConfiguration _config;
    private readonly IUserInformationService _userInformationService;

    public GovUkNotifyService(GovUkNotifyConfiguration config, IUserInformationService UserInformationService)
    {
        _config = config;
        _userInformationService = UserInformationService;
    }

    public async Task<bool> SendEmailAccountCreation()
    {
        string userUpn = _userInformationService.GetCurrentUserUpn();
        return await SendEmail(userUpn, _config.TemplateIds.AccountCreation);
    }

    public async Task<bool> SendEmailApplicationSubmitted()
    {
        string userUpn = _userInformationService.GetCurrentUserUpn();
        return await SendEmail(userUpn, _config.TemplateIds.ApplicationSubmitted);
    }

    public async Task<bool> SendEmailInformationFromPreEngagement()
    {
        string userUpn = _userInformationService.GetCurrentUserUpn();
        return await SendEmail(userUpn, _config.TemplateIds.InformationFromPreEngagement);
    }

    private async Task<bool> SendEmail(string outboundEmailAddress, string templateId, Dictionary<string, object>? personalisation = null)
    {
        try
        {
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );

            await retryPolicy.ExecuteAsync(async () =>
            {
                var client = new NotificationClient(_config.ApiKey);
                await client.SendEmailAsync(outboundEmailAddress, templateId, personalisation);
            });

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Gov UK Notify email with templateId {TemplateId} was not sent successfully", templateId);
            return false;
        }
    }
}