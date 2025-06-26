namespace Ofqual.Recognition.Citizen.API.Infrastructure.Services.Interfaces;

public interface IFeatureFlagService
{
    public bool IsFeatureEnabled(string featureName);
}
