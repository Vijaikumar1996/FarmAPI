using FarmAPI.Data;
using FarmAPI.Interface;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static FarmAPI.DTOs.LoginDto;

namespace FarmAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly FarmDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(
            FarmDbContext context,
            IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

 
        public async Task<LoginResponseDto?> LoginAsync(
    LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UserName))
                throw new Exception("Username is required");

            if (string.IsNullOrWhiteSpace(request.Password))
                throw new Exception("Password is required");

            var user = await _context.Users
                .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
                .FirstOrDefaultAsync(x =>
                    x.UserName == request.UserName);

            if (user == null)
                throw new Exception("Invalid username or password");

            if (!user.IsActive)
                throw new Exception("User account is inactive");

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
                throw new Exception("Password not configured");

            //var isValidPassword =
            //    BCrypt.Net.BCrypt.Verify(
            //        request.Password,
            //        user.PasswordHash);

            //if (!isValidPassword)
            //    throw new Exception("Invalid username or password");
            if (user.PasswordHash != request.Password)
            {
                throw new ValidationException("Invalid email or password.");
            }

            var roles = user.UserRoles
                .Select(x => x.Role.RoleName)
                .ToList();

            if (!roles.Any())
                throw new Exception("No roles assigned to the user");

            user.LastLoginAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new LoginResponseDto
            {                
                FullName = user.FullName,
                AccessToken = _jwtService.GenerateToken(user),
                Roles = roles
            };
        }
    }
}
