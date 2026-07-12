using FarmAPI.Entities;

namespace FarmAPI.Interface
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
