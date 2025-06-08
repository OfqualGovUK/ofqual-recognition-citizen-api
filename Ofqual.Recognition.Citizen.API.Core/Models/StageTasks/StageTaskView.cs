
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class StageTaskView
{
    public StageEnum StageId { get; set; }
    public string Stage { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public string Task { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
}