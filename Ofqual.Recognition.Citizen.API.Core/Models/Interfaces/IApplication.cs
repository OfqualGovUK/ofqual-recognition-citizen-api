namespace Ofqual.Recognition.Citizen.API.Core.Models.Interfaces;

public interface IApplication
{
    public Guid ApplicationId { get; set; }
    public Guid OwnerUserId { get; set; }
}