namespace race_day_backend_api.Models.Auth
{
    public class RegisterRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Participant";

        public string? ImageUrl { get; set; }

        public string? Gender { get; set; }

        public int? Age { get; set; }

        public string? AccessLevel { get; set; }
    }
}
