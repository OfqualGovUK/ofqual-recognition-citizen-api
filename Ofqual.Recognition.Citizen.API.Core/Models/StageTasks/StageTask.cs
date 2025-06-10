
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class StageTask
{
    public Guid StageTaskId { get; set; }
    public StageEnum StageId { get; set; }
    public Guid TaskId { get; set; }
    public int OrderNumber { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string CreatedByUpn { get; set; } = string.Empty;
    public string ModifiedByUpn { get; set; } = string.Empty;
}
