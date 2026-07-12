using FarmAPI.Entities;
using System.Security.Claims;
using static FarmAPI.DTOs.LoginDto;

namespace FarmAPI.Interface
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(
            LoginRequestDto request);

    }
}
