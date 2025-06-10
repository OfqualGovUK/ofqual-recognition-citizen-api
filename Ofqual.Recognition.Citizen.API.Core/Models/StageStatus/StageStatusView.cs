using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class StageStatusView
{
    public Guid ApplicationId { get; set; }
    public Stage StageId { get; set; }
    public required string StageName { get; set; }
    public TaskStatusEnum StatusId { get; set; }
    public required string Status { get; set; }
    public DateTime StageStartDate { get; set; }
    public DateTime? StageCompletionDate { get; set; }
}