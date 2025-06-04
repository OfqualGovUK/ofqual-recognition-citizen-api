using Ofqual.Recognition.Citizen.API.Core.Models;
using Ofqual.Recognition.Citizen.API.Core.Enums;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class AntiVirusMapper
{
    /// <summary>
    /// Maps a <see cref="AttachmentScannerResult"/> data model to a <see cref="VirusScan"/>.
    /// </summary>
    public static VirusScan MapToVirusScan(AttachmentScannerResult result)
    {
        return new VirusScan
        {
            ScanId = result.Id,
            Outcome = result.Status switch
            {
                "ok" => VirusScanOutcome.Clean,
                "pending" => VirusScanOutcome.Pending,
                "infected" => VirusScanOutcome.Infected,
                "error" => VirusScanOutcome.ScanFailed,
                _ => VirusScanOutcome.ScanFailed
            }
        };
    }
}