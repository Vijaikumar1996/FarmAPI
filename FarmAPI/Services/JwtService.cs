namespace FarmAPI.Services
{
    using FarmAPI.Entities;
    using FarmAPI.Interface;
    using Microsoft.IdentityModel.Tokens;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(
            IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(
            User user)
        {
            var claims = new List<Claim>
{
    new Claim(
        ClaimTypes.NameIdentifier,
        user.Id.ToString()),

    new Claim(
        ClaimTypes.Name,
        user.FullName),

    new Claim(
        "username",
        user.UserName)
};

            foreach (var role in user.UserRoles)
            {
                claims.Add(
                    new Claim(
                        ClaimTypes.Role,
                        role.Role.RoleName));
            }

            var key =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        _configuration["Jwt:Key"]!));

            var creds =
                new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256);

            var token =
                new JwtSecurityToken(
                    issuer:
                        _configuration["Jwt:Issuer"],

                    audience:
                        _configuration["Jwt:Audience"],

                    claims:
                        claims,

                    expires:
                        DateTime.UtcNow.AddDays(
                            Convert.ToInt32(
                                _configuration["Jwt:ExpiryDays"])),

                    signingCredentials:
                        creds);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}
