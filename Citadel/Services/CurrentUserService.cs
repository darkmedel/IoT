namespace cl.MedelCodeFactory.IoT.Citadel.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    public string GetCurrentUser()
    {
        return "system";
    }
}
