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
    private readonly GovUkNotifyConfiguration _config;

    public GovUkNotifyService(GovUkNotifyConfiguration config)
    {
        _config = config;
    }

    public async Task<bool> SendEmail(string outboundEmailAddress) 
    {
        try 
        {
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
                var client = new NotificationClient(_config.GovUkApiKey);

                await Task.Run(() => { EmailNotificationResponse repsonse = client.SendEmail(outboundEmailAddress, _config.TemplateId); });
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
