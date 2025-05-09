namespace AuthService.Models
{
    public class SignupRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Position { get; set; }
        public string? Department { get; set; }
        public string Email { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = null!;
        public DateTime Expiry { get; set; }
    }
}
