// File: Controllers/Api/AuthController.cs

using ERMS.DTOs;
using ERMS.Models;
using Microsoft.AspNetCore.Authentication.Cookies; // Needed for specific scheme constant
using Microsoft.AspNetCore.Authorization;          // Needed for [Authorize] attribute
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ERMS.Controllers.Api
{
    /// <summary>
    /// API Controller responsible for user authentication and JWT generation.
    /// </summary>
    [Route("api/[controller]")] // -> /api/Auth
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthController"/> class.
        /// </summary>
        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // POST: api/Auth/token
        /// <summary>
        /// Authenticates a user via credentials and returns a JWT if successful. (Used by client-side login form)
        /// </summary>
        /// <param name="loginDto">The user's login credentials.</param>
        /// <returns>An access token or an Unauthorized response.</returns>
        [HttpPost("token")]
        [AllowAnonymous] // Allow anonymous access to get the initial token
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetToken([FromBody] LoginDto loginDto)
        {
            _logger.LogInformation($"Token requested via credentials for user: {loginDto.Email}");
            try
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    _logger.LogWarning($"Authentication failed: User {loginDto.Email} not found.");
                    return Unauthorized(new { message = "Invalid credentials." });
                }
                var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
                if (!result.Succeeded)
                {
                    if (result.IsLockedOut) { /* ... Log and return locked out ... */ return Unauthorized(new { message = "Account locked out." }); }
                    if (result.IsNotAllowed) { /* ... Log and return not allowed ... */ return Unauthorized(new { message = "Login not allowed." }); }
                    _logger.LogWarning($"Authentication failed: Invalid password attempt for user {loginDto.Email}.");
                    return Unauthorized(new { message = "Invalid credentials." });
                }
                _logger.LogInformation($"User {loginDto.Email} authenticated successfully via credentials.");
                // --- GENERATE JWT (Common logic) ---
                var tokenResponse = await GenerateJwtTokenResponse(user);
                if (tokenResponse == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error generating token response.");
                }
                // --- END JWT GENERATION ---
                _logger.LogInformation($"JWT generated successfully for user {loginDto.Email} via credentials.");
                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred during token generation via credentials for user {loginDto.Email}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred during authentication.");
            }
        }


        // *** START NEW METHOD ***
        // GET: api/Auth/get-my-token
        /// <summary>
        /// Gets a JWT for the currently authenticated (via cookie) user.
        /// Used by client-side scripts after a successful cookie-based login.
        /// </summary>
        /// <returns>An access token or Unauthorized if the user is not logged in via cookie.</returns>
        [HttpGet("get-my-token")]
        // --- IMPORTANT: Authorize based on the COOKIE scheme ---
        // This ensures only users already logged in via MVC/Identity cookie can get a token this way.
        [Authorize(AuthenticationSchemes = "Identity.Application")] // Use the literal string value
        [ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)] // If cookie is invalid/missing
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyToken()
        {
            // User is authenticated via cookie because of the [Authorize] attribute above.
            // Get the user object based on the cookie principal.
            var user = await _userManager.GetUserAsync(User); // User comes from HttpContext via ControllerBase

            if (user == null)
            {
                // Should not happen if [Authorize] is working correctly, but handle defensively.
                _logger.LogWarning("GetMyToken called, but User could not be found from cookie principal. Cookie might be invalid or user deleted.");
                // Return 401 as the cookie didn't represent a valid logged-in user session
                return Unauthorized(new { message = "User session not found or invalid." });
            }

            _logger.LogInformation("Generating token via GetMyToken for cookie-authenticated user: {UserId}", user.Id);

            try
            {
                // --- GENERATE JWT (Common logic) ---
                var tokenResponse = await GenerateJwtTokenResponse(user);
                if (tokenResponse == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, "Error generating token response.");
                }
                // --- END JWT GENERATION ---

                _logger.LogInformation("Token generated successfully via GetMyToken for user {UserId}", user.Id);
                return Ok(tokenResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token via GetMyToken for user {UserId}", user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error generating token.");
            }
        }
        // *** END NEW METHOD ***


        // *** START HELPER METHOD FOR JWT GENERATION ***
        /// <summary>
        /// Generates the JWT Token Response DTO for a given user.
        /// </summary>
        /// <param name="user">The user to generate the token for.</param>
        /// <returns>TokenResponseDto or null if configuration is missing.</returns>
        private async Task<TokenResponseDto?> GenerateJwtTokenResponse(User user)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim> {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName), // Use UserName or Email for Sub
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token identifier
                new Claim("firstname", user.FirstName ?? ""), // Custom claim
                new Claim("lastname", user.LastName ?? "")     // Custom claim
            };
            foreach (var userRole in userRoles) { authClaims.Add(new Claim(ClaimTypes.Role, userRole)); }

            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
            {
                _logger.LogError("JWT Key, Issuer, or Audience is missing in configuration.");
                return null; // Return null to indicate configuration error
            }
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            var token = new JwtSecurityToken(
                 issuer: jwtIssuer,
                 audience: jwtAudience,
                 expires: DateTime.UtcNow.AddHours(3), // Adjust expiry as needed
                 claims: authClaims,
                 signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return new TokenResponseDto
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Expiration = token.ValidTo
            };
        }
        // *** END HELPER METHOD ***


    } // End of AuthController class

    // --- Helper DTOs --- (Keep these at the bottom or move to DTOs folder)

    /// <summary>
    /// Represents the data required for a user to log in.
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the data returned after successful authentication.
    /// </summary>
    public class TokenResponseDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;
        [Required]
        public DateTime Expiration { get; set; }
    }
} // End of namespace