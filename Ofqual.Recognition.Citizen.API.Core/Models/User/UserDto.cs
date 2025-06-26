namespace Ofqual.Recognition.Citizen.API.Core.Models;

public class UserDto
{
    public Guid UserId { get; set; }
    public Guid B2CId { get; set; }
    public required string EmailAddress { get; set; }
    public required string DisplayName { get; set; }
}