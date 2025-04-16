using Ofqual.Recognition.Citizen.API.Core.Models;

namespace Ofqual.Recognition.Citizen.API.Core.Mappers;

/// <summary>
/// Provides mapping functions for application-related models.
/// </summary>
public static class ApplicationMapper
{
    /// <summary>
    /// Maps an <see cref="Application"/> model to an <see cref="ApplicationDetailsDto"/>.
    /// </summary>
    /// <param name="application">The application domain model to map.</param>
    /// <returns>The mapped <see cref="ApplicationDetailsDto"/>.</returns>
    public static ApplicationDetailsDto ToDto(Application application)
    {
        return new ApplicationDetailsDto
        {
            ApplicationId = application.ApplicationId
        };
    }
}