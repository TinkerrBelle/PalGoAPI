// Controllers/AuthController.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PalGoAPI.DTOs;
using PalGoAPI.Models;
using PalGoAPI.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace PalGoAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;


        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _configuration = configuration;
            _emailService = emailService;
        }

        //[HttpPost("register")]
        //public async Task<IActionResult> Register([FromBody] RegisterDto model)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    var existingUser = await _userManager.FindByEmailAsync(model.Email);
        //    if (existingUser != null)
        //        return BadRequest(new { message = "User with this email already exists" });

        //    var user = new ApplicationUser
        //    {
        //        UserName = model.Email,
        //        Email = model.Email,
        //        FirstName = model.FirstName,
        //        LastName = model.LastName,
        //        PhoneNumber = model.PhoneNumber,
        //        Role = model.Role,
        //        CreatedAt = DateTime.UtcNow,
        //        IsActive = true
        //    };

        //    var result = await _userManager.CreateAsync(user, model.Password);

        //    if (!result.Succeeded)
        //    {
        //        return BadRequest(new { message = "Registration failed", errors = result.Errors });
        //    }

        //    // Get device info
        //    var deviceInfo = Request.Headers["User-Agent"].ToString();
        //    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        //    // Generate both tokens
        //    var token = _tokenService.GenerateAccessToken(user);
        //    var refresh = await _tokenService.GenerateRefreshToken(user.Id, deviceInfo, ipAddress);

        //    var tokenExpiry = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["JwtSettings:ExpiryInMinutes"]));
        //    var refreshExpiry = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["JwtSettings:RefreshTokenExpiryInDays"]));

        //    return Ok(new
        //    {
        //        accessToken = token,
        //        refreshToken = refresh,
        //        accessTokenExpiry = tokenExpiry,
        //        refreshTokenExpiry = refreshExpiry,
        //        user = new
        //        {
        //            id = user.Id,
        //            email = user.Email,
        //            firstName = user.FirstName,
        //            lastName = user.LastName,
        //            role = user.Role.ToString()
        //        }
        //    });
        //}

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { message = "User with this email already exists" });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsEmailVerified = 0  // Not verified yet
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(new { message = "Registration failed", errors = result.Errors });

            // Generate 4-digit code
            var code = new Random().Next(1000, 9999).ToString();
            user.EmailVerificationCode = code;
            user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(10); // Code expires in 10 mins
            await _userManager.UpdateAsync(user);

            // Try sending email but don't crash if it fails
            try
            {
                await _emailService.SendEmailAsync(
                    user.Email,
                    "Verify your PalGo account",
                    $@"
                    <h2>Welcome to PalGo!</h2>
                    <p>Your verification code is:</p>
                    <h1 style='font-size: 48px; letter-spacing: 10px; color: #2563eb;'>{code}</h1>
                    <p>This code expires in 10 minutes.</p>
                    "
                );
            }
            catch (Exception emailEx)
            {
                // Log it but don't fail the registration
                Console.WriteLine($"Email sending failed: {emailEx.Message}");
                // During development you can log the code so you can test manually
                Console.WriteLine($"VERIFICATION CODE for {user.Email}: {code}");
            }
            return Ok(new
            {
                message = "Registration successful. Please check your email for verification code.",
                email = user.Email  // Send back email so frontend knows where to verify
            });
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.IsEmailVerified == 1)
                return BadRequest(new { message = "Email already verified" });

            if (user.EmailVerificationCode != model.Code)
                return BadRequest(new { message = "Invalid verification code" });

            if (user.EmailVerificationExpiry < DateTime.UtcNow)
                return BadRequest(new { message = "Verification code has expired. Please request a new one." });

            // Mark as verified and clear the code
            user.IsEmailVerified = 1;
            user.EmailVerificationCode = null;
            user.EmailVerificationExpiry = null;
            await _userManager.UpdateAsync(user);

            return Ok(new { message = "Email verified successfully!" });
        }

        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendVerificationCode([FromBody] ResendCodeDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.IsEmailVerified == 1)
                return BadRequest(new { message = "Email already verified" });

            // Generate new code
            var code = new Random().Next(1000, 9999).ToString();
            user.EmailVerificationCode = code;
            user.EmailVerificationExpiry = DateTime.UtcNow.AddMinutes(10);
            await _userManager.UpdateAsync(user);

            await _emailService.SendEmailAsync(
                user.Email,
                "Your new PalGo verification code",
                $@"
        <h2>New Verification Code</h2>
        <p>Your new verification code is:</p>
        <h1 style='font-size: 48px; letter-spacing: 10px; color: #2563eb;'>{code}</h1>
        <p>This code expires in 10 minutes.</p>
        "
            );

            return Ok(new { message = "New verification code sent!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return Unauthorized(new { message = "Invalid email or password" });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Account is deactivated" });

            // Get device info
            var deviceInfo = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Generate both tokens
            var token = _tokenService.GenerateAccessToken(user);
            var refresh = await _tokenService.GenerateRefreshToken(user.Id, deviceInfo, ipAddress);

            var tokenExpiry = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["JwtSettings:ExpiryInMinutes"]));
            var refreshExpiry = DateTime.UtcNow.AddDays(Convert.ToInt32(_configuration["JwtSettings:RefreshTokenExpiryInDays"]));

            return Ok(new
            {
                accessToken = token,
                refreshToken = refresh,
                accessTokenExpiry = tokenExpiry,
                refreshTokenExpiry = refreshExpiry,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role.ToString()
                }
            });
        }

        [HttpPost("login-simple")]
        public async Task<IActionResult> LoginSimple([FromBody] LoginDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return Unauthorized(new { message = "Invalid email or password" });

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

                if (!result.Succeeded)
                    return Unauthorized(new { message = "Invalid email or password" });

                if (!user.IsActive)
                    return Unauthorized(new { message = "Account is deactivated" });

                // Generate ONLY access token (no refresh token)
                var token = _tokenService.GenerateAccessToken(user);
                var tokenExpiry = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["JwtSettings:ExpiryInMinutes"]));

                return Ok(new
                {
                    accessToken = token,
                    refreshToken = "not-implemented",  // Placeholder
                    accessTokenExpiry = tokenExpiry,
                    refreshTokenExpiry = tokenExpiry,
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = user.Role.ToString()
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(new { message = "Refresh token is required" });

            var refreshToken = await _tokenService.ValidateRefreshToken(request.RefreshToken);

            if (refreshToken == null)
                return Unauthorized(new { message = "Invalid or expired refresh token" });

            // Generate new access token
            var newAccessToken = _tokenService.GenerateAccessToken(refreshToken.User);
            var tokenExpiry = DateTime.UtcNow.AddMinutes(Convert.ToInt32(_configuration["JwtSettings:ExpiryInMinutes"]));

            return Ok(new
            {
                accessToken = newAccessToken,
                accessTokenExpiry = tokenExpiry
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                await _tokenService.RevokeRefreshToken(request.RefreshToken);
            }

            return Ok(new { message = "Logged out successfully" });
        }

        [Authorize]
        [HttpPost("logout-all")]
        public async Task<IActionResult> LogoutAll()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await _tokenService.RevokeAllUserRefreshTokens(userId);

            return Ok(new { message = "Logged out from all devices successfully" });
        }


        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                // Try to get userId from the full claim path first
                var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    // Fallback to ClaimTypes.NameIdentifier
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    // Last resort: try sub claim
                    userId = User.FindFirst("sub")?.Value;
                }

                Console.WriteLine($"Extracted userId: {userId}");

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { message = "User ID not found in token" });
                }

                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    Console.WriteLine($"User not found in database for ID: {userId}");
                    return NotFound(new { message = "User not found", userId = userId });
                }

                Console.WriteLine($"User found: {user.Email}");

                return Ok(new
                {
                    id = user.Id,
                    email = user.Email,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    role = user.Role.ToString(),
                    phoneNumber = user.PhoneNumber,
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Profile error: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }
        // Add to AuthController temporarily
        [Authorize]
        [HttpGet("test-claims")]
        public IActionResult TestClaims()
        {
            var claims = User.Claims.Select(c => new
            {
                Type = c.Type,
                Value = c.Value
            }).ToList();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userIdAlt = User.FindFirst("sub")?.Value;

            return Ok(new
            {
                claims = claims,
                nameIdentifier = userId,
                sub = userIdAlt,
                isAuthenticated = User.Identity?.IsAuthenticated
            });
        }

        [HttpGet("db-test")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                var walletLocation = _configuration["Oracle:WalletLocation"];
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                var walletExists = Directory.Exists(walletLocation);
                var walletFiles = walletExists ? Directory.GetFiles(walletLocation) : new string[0];

                // Try to connect by finding a user
                ApplicationUser testUser = null;
                bool canConnect = false;
                string dbError = null;

                try
                {
                    testUser = await _userManager.Users.FirstOrDefaultAsync();
                    canConnect = true;
                }
                catch (Exception dbEx)
                {
                    dbError = dbEx.Message;
                }

                return Ok(new
                {
                    walletLocation = walletLocation,
                    walletExists = walletExists,
                    walletFiles = walletFiles.Select(Path.GetFileName).ToArray(),
                    canConnect = canConnect,
                    userCount = canConnect ? await _userManager.Users.CountAsync() : 0,
                    dbError = dbError,
                    environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    connectionStringMasked = connectionString?.Split(';')[0] // Only show first part
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [HttpGet("list-users")]
        public async Task<IActionResult> ListUsers()
        {
            var users = await _userManager.Users
                .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
                .Take(10)
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            return Ok(new
            {
                version = "2.0-with-refresh-tokens",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("test-deployment")]
        public IActionResult TestDeployment()
        {
            return Ok(new
            {
                message = "NEW CODE DEPLOYED - Feb 20 11:35",
                timestamp = DateTime.UtcNow,
                version = "v2.0"
            });
        }
    }

    // DTO for refresh token request
    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; }
    }
}