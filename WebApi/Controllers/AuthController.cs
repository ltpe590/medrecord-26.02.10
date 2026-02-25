using Core.DTOs;
using Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public class LoginModel
        {
            public required string Username { get; set; }
            public required string Password { get; set; }
        }

        public class RegisterModel : LoginModel
        {
            public required string Email { get; set; }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var user = new AppUser { UserName = model.Username, Email = model.Email };
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { Message = "User registered successfully" });
            }
            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                Console.WriteLine($"=== LOGIN ATTEMPT ===");
                Console.WriteLine($"Username: {model.Username}");

                // 1. Try to find user
                var user = await _userManager.FindByNameAsync(model.Username);
                Console.WriteLine($"User found: {user != null}");

                if (user == null)
                {
                    Console.WriteLine($"ERROR: User '{model.Username}' not found in database");
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                Console.WriteLine($"User details - ID: {user.Id}, Email: {user.Email}, IsActive: {user.IsActive}");

                // 2. Check if user is active
                if (!user.IsActive)
                {
                    Console.WriteLine($"ERROR: User '{model.Username}' is inactive");
                    return Unauthorized(new { Message = "Account is deactivated" });
                }

                // 3. Check password
                var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
                Console.WriteLine($"Password valid: {passwordValid}");

                if (!passwordValid)
                {
                    Console.WriteLine($"ERROR: Password incorrect for '{model.Username}'");
                    return Unauthorized(new { Message = "Invalid credentials" });
                }

                // 4. Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                // 5. Generate token
                var token = GenerateJwtToken(user);
                Console.WriteLine($"Login SUCCESS for '{model.Username}'");

                return Ok(new LoginResponse { Token = token, Username = user?.UserName ?? "Unknown Username" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LOGIN EXCEPTION: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { Message = "Login failed due to server error" });
            }
        }

        [HttpGet("debug-appuser")]
        public async Task<IActionResult> DebugAppUser()
        {
            try
            {
                // 1. Check what type we're using
                var userManagerType = _userManager.GetType().GetGenericArguments().FirstOrDefault();

                // 2. Try to find admin
                var admin = await _userManager.FindByNameAsync("admin");

                // 3. Check if properties exist
                var hasIsActive = admin?.GetType().GetProperty("IsActive") != null;
                var hasFingerprintTemplate = admin?.GetType().GetProperty("FingerprintTemplate") != null;

                return Ok(new
                {
                    UserManagerType = userManagerType?.FullName,
                    ExpectedType = typeof(AppUser).FullName,
                    AdminFound = admin != null,
                    AdminId = admin?.Id,
                    AdminUserName = admin?.UserName,
                    HasIsActiveProperty = hasIsActive,
                    HasFingerprintTemplateProperty = hasFingerprintTemplate,
                    IsActiveValue = hasIsActive && admin != null ? admin?.GetType().GetProperty("IsActive")?.GetValue(admin) : "N/A",
                    DatabaseMismatch = hasIsActive ? "OK" : "PROBLEM: AppUser doesn't match database"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpPost("debug-password")]
        public async Task<IActionResult> DebugPassword([FromBody] LoginModel model)
        {
            try
            {
                var user = await _userManager.FindByNameAsync(model.Username);

                if (user == null)
                    return NotFound(new { Message = "User not found" });

                // Use UserManager's CheckPasswordAsync - it handles hashing correctly
                var isValid = await _userManager.CheckPasswordAsync(user, model.Password);

                // Check hash format for debugging only
                string hashFormat = "Unknown";
                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    if (user.PasswordHash.StartsWith("AQAAAAIAAYagAAAAE"))
                        hashFormat = "ASP.NET Identity V3";
                    else if (user.PasswordHash.StartsWith("AEAAAA"))
                        hashFormat = "ASP.NET Identity V2";
                    else if (user.PasswordHash.Length == 88 && user.PasswordHash.EndsWith("=="))
                        hashFormat = "PBKDF2 with HMAC-SHA256";
                    else if (user.PasswordHash.Length == 44)
                        hashFormat = "Base64 encoded";
                }

                return Ok(new
                {
                    Username = user.UserName,
                    PasswordHashPreview = user.PasswordHash?.Substring(0, Math.Min(30, user.PasswordHash?.Length ?? 0)) + "...",
                    PasswordHashLength = user.PasswordHash?.Length,
                    PasswordValid = isValid,
                    HashFormat = hashFormat,
                    Note = "ASP.NET Identity handles password hashing automatically"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        private string GenerateJwtToken(AppUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? user.UserName ?? user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var jwtKey = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is not configured");
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expireDaysConfig = _configuration["Jwt:ExpireDays"];
            var expires = DateTime.Now.AddDays(Convert.ToDouble(string.IsNullOrEmpty(expireDaysConfig) ? "1" : expireDaysConfig));

            var token = new JwtSecurityToken(
                issuer: "yourdomain.com",
                audience: "yourdomain.com",
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}