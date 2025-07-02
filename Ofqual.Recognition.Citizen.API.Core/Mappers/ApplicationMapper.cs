using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class ApplicationMapper
{
    /// <summary>
    /// Maps a <see cref="Application"/> data model to a <see cref="ApplicationDetailsDto"/>.
    /// </summary>
    public static ApplicationDetailsDto ToDto(Application application)
    {
        return new ApplicationDetailsDto
        {
            ApplicationId = application.ApplicationId,
            OwnerUserId = application.OwnerUserId,
            Submitted = application.SubmittedDate.HasValue
        };
    }
}