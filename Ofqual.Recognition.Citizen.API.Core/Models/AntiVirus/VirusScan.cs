using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class VirusScan
{
    public string ScanId { get; set; } = string.Empty;
    public VirusScanOutcome Outcome { get; set; }
}