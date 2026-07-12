namespace FarmAPI.Interface
{   

    public interface ICurrentUserService
    {
        long UserId { get; }

        string UserName { get; }

        List<string> Roles { get; }
    }
}
