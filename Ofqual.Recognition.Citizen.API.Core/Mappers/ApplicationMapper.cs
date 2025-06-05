using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

public static class ApplicationMapper
{

    /// <summary>
    /// Maps a collection of <see cref="Application"/> to a list of <see cref="ApplicationDetailsDto"/>.
    /// </summary>
    public static ApplicationDetailsDto ToDto(Application application)
    {
        return new ApplicationDetailsDto
        {
            ApplicationId = application.ApplicationId
        };
    }
}