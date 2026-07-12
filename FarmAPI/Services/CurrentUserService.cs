using FarmAPI.Interface;
using System.Security.Claims;

namespace FarmAPI.Services
{


    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long UserId
        {
            get
            {
                var value = _httpContextAccessor
                    .HttpContext?
                    .User
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                return string.IsNullOrEmpty(value)
                    ? 0
                    : long.Parse(value);
            }
        }

        public string UserName =>
            _httpContextAccessor
                .HttpContext?
                .User
                .FindFirstValue("username")
            ?? string.Empty;

        public List<string> Roles =>
            _httpContextAccessor
                .HttpContext?
                .User
                .FindAll(ClaimTypes.Role)
                .Select(x => x.Value)
                .ToList()
            ?? new List<string>();
    }
}
