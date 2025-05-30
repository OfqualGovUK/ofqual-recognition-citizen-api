using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IAntiVirusService
{
    public Task<VirusScan> ScanFile(Stream fileStream, string fileName);
}
