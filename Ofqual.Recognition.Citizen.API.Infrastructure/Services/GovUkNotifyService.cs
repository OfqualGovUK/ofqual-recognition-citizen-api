using Notify.Client;
using Notify.Models.Responses;
using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;
using Polly;
using Serilog;
using static System.Net.Mime.MediaTypeNames;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services;

public class GovUkNotifyService : IGovUkNotifyService
{
    private readonly string _govUkApiKey;
    private readonly string _templateId;
    private readonly string _oneGovUkSignIn;

    public GovUkNotifyService(GovUkNotifyConfiguration config)
    {
        _govUkApiKey = config.GovUkApiKey;
        _templateId = config.TemplateId;
        _oneGovUkSignIn = config.OneGovUkSignIn;
    }

    public async Task<bool> SendEmail(string outboundEmailAddress) 
    {
        try 
        {
            Dictionary<string, object> emailPersonalisation = new()
            {
                { "sign_in_url", _oneGovUkSignIn }
            };

            await Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            })
            .ExecuteAsync(async () =>
            {
                var client = new NotificationClient(_govUkApiKey);

                await Task.Run(() => { EmailNotificationResponse repsonse = client.SendEmail(outboundEmailAddress, _templateId, emailPersonalisation); });
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
