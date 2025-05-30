namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class VirusScan
{
    public bool IsOk { get; set; }
    public bool IsPending { get; set; }
    public string ScanId { get; set; }
}