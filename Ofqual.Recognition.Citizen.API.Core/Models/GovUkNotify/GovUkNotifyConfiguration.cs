namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class GovUkNotifyConfiguration
{
    public required string ApiKey { get; set; }
    public required string RecognitionEmailInbox { get; set; }
    public required GovUkNotifyTemplateIds TemplateIds { get; set; }
}