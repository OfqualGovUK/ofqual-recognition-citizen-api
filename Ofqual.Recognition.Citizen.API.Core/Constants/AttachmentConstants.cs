namespace Ofqual.Recognition.Citizen.API.Core.Constants;

public static class AttachmentConstants
{
    public const int MaxFileSizeMb = 25;
    public const int MaxTotalSizeMb = 100;

    public const long MaxFileSizeBytes = MaxFileSizeMb * 1024 * 1024;
    public const long MaxTotalSizeBytes = MaxTotalSizeMb * 1024 * 1024;
}
