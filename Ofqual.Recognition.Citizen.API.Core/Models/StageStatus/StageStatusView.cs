using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class StageStatusView
{
    public Guid ApplicationId { get; set; }
    public StageType StageId { get; set; }
    public required string StageName { get; set; }
    public StatusType StatusId { get; set; }
    public required string Status { get; set; }
    public DateTime StageStartDate { get; set; }
    public DateTime? StageCompletionDate { get; set; }
}