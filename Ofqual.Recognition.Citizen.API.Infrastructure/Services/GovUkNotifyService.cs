using Notify.Client;
using Notify.Models.Responses;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
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
        if (_config.TemplateIds?.AccountCreation == null)
        {
            Log.Error("Gov UK Notify template ID for account creation is not configured.");
            return false;
        }
        
        string userUpn = _userInformationService.GetCurrentUserUpn();
        return await SendEmail(userUpn, _config.TemplateIds.AccountCreation);
    }

    public async Task<bool> SendEmailRequestPreEngagement()
    {
        if (string.IsNullOrEmpty(_config.TemplateIds.RequestPreEngagement))
        {
            Log.Error("Gov UK Notify template ID for request pre-engagement is not configured.");
            return false;
        }
        string userUpn = _userInformationService.GetCurrentUserUpn();
        return await SendEmail(userUpn, _config.TemplateIds.RequestPreEngagement);
    }

    private async Task<bool> SendEmail(string outboundEmailAddress, string templateId, Dictionary<string, object>? personalisation = null)
    {
        try
        {
            await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(new List<TimeSpan>
            {
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            })
            .ExecuteAsync(async () =>
            {
                var client = new NotificationClient(_config.ApiKey);

                await Task.Run(() => { EmailNotificationResponse repsonse = client.SendEmail(outboundEmailAddress, templateId, personalisation); });
            });

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Log.Warning("Gov UK Notify email was not sent successfully");

            return false;
        }
    }
}
