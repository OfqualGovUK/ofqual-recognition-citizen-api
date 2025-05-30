using Ofqual.Recognition.Citizen.API.Core.Models;

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
            IsOk = result.Status == "ok",
            IsPending = result.Status == "pending",
            ScanId = result.Id
        };
    }
}