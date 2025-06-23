public interface IUserInformationService
{
    public string GetCurrentUserObjectId();
    public string GetCurrentUserDisplayName();
    public string GetCurrentUserUpn();
    public Task<bool> CheckUserCanModifyApplication(string applicationId);
    public Task<bool> CheckUserCanModifyApplication(Guid applicationId);
}

