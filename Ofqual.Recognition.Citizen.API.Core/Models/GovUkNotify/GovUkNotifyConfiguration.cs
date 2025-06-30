namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class GovUkNotifyConfiguration
{
    public string GovUkApiKey { get; set; } = string.Empty;
    public GovUkNotifyTemplateIds? TemplateIds { get; set; }
}