namespace FarmAPI.DTOs
{
    public class LoginDto
    {
        public class LoginRequestDto
        {
            public string UserName { get; set; }
                = string.Empty;

            public string Password { get; set; }
                = string.Empty;
        }

        public class LoginResponseDto
        {
          
            public string FullName { get; set; }
                = string.Empty;

            public string AccessToken { get; set; }
                = string.Empty;

            public List<string> Roles { get; set; }
                = new();
        }
    }
}
