﻿using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class StageStatus
{
    public Guid StageStatusId { get; set; }
    public Guid ApplicationId { get; set; }
    public StageType StageId { get; set; }
    public StatusType StatusId { get; set; }
    public DateTime StageStartDate { get; set; }
    public DateTime? StageCompletionDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public required string CreatedByUpn { get; set; }
    public string? ModifiedByUpn { get; set; }
}