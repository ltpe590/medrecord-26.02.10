using Core.DTOs;
using Core.Entities;

namespace Core.Interfaces.Services
{
    public interface IUserMappingService
    {
        UserDto MapToDto(AppUser domainUser);
        AppUser MapToDomain(UserCreateDto dto);
        UserCreateDto MapToCreateDto(string username, string email, string PhoneNumber, string password);
    }

    public class UserMappingService : IUserMappingService
    {
        public UserDto MapToDto(AppUser domainUser)
        {
            return new UserDto
            {
                Id = domainUser.Id,
                UserName = domainUser?.UserName ?? string.Empty,
                Email = domainUser?.Email ?? string.Empty,
                EmailConfirmed = true, // Domain users are confirmed by default
                PhoneNumber = domainUser?.PhoneNumber ?? string.Empty,
                PhoneNumberConfirmed = !string.IsNullOrEmpty(domainUser?.PhoneNumber),
                CreatedAt = domainUser?.CreatedAt ?? DateTime.MinValue,
                LastLoginAt = domainUser?.LastLoginAt ?? DateTime.MinValue,
                HasFingerprintEnrolled = domainUser?.HasFingerprintEnrolled ?? false
            };
        }

        public AppUser MapToDomain(UserCreateDto dto)
        {
            return new AppUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                HasFingerprintEnrolled = false
            };
        }

        public UserCreateDto MapToCreateDto(string username, string email, string phonenumber, string password)
        {
            return new UserCreateDto
            {
                UserName = username,
                Email = email,
                PhoneNumber = phonenumber,
                Password = password
            };
        }
    }
}
