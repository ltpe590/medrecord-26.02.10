namespace Core.DTOs
{
    public class UserDto
    {
        public string Id { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool EmailConfirmed { get; init; }
        public string PhoneNumber { get; init; } = string.Empty;
        public bool PhoneNumberConfirmed { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public bool HasFingerprintEnrolled { get; init; }
    }

    public class UserCreateDto
    {
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
    }

    public class UserResponseDto
    {
        public string Id { get; init; } = string.Empty;
        public string UserName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool EmailConfirmed { get; init; }
        public string PhoneNumber { get; init; } = string.Empty;
        public bool PhoneNumberConfirmed { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public bool HasFingerprintEnrolled { get; init; }
        public string FingerprintStatus { get; init; } = "Not enrolled";
    }
}