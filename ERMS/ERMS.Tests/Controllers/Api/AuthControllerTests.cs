using Xunit;
using Moq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using ERMS.Controllers.Api;
using ERMS.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace ERMS.Tests.Controllers.Api
{
    public class AuthControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;
        private readonly List<User> _testUsers;

        public AuthControllerTests()
        {
            // Setup test users
            _testUsers = new List<User>
            {
                new User
                {
                    Id = "user1",
                    UserName = "user1@example.com",
                    Email = "user1@example.com",
                    FirstName = "Test",
                    LastName = "User"
                },
                new User
                {
                    Id = "user2",
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User"
                }
            };

            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Setup UserManager methods
            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => _testUsers.Find(u => u.Email == email));

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((ClaimsPrincipal principal) =>
                {
                    var nameIdentifier = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                    return _testUsers.Find(u => u.Id == nameIdentifier);
                });

            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) =>
                {
                    if (user.Id == "user1") return new List<string> { "Employee" };
                    if (user.Id == "user2") return new List<string> { "Admin", "Manager" };
                    return new List<string>();
                });

            // Setup SignInManager mock
            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                null, null, null, null);

            // Setup SignInManager methods
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((User user, string password, bool lockoutOnFailure) =>
                {
                    if (password == "Password123!") return Microsoft.AspNetCore.Identity.SignInResult.Success;
                    return Microsoft.AspNetCore.Identity.SignInResult.Failed;
                });

            // Setup IConfiguration mock
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns("YourSecureSecretKeyWithAtLeast32Characters");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("ERMS");
            _mockConfiguration.Setup(c => c["Jwt:Audience"]).Returns("ERMSUsers");

            // Setup Logger mock
            _mockLogger = new Mock<ILogger<AuthController>>();

            // Create controller instance
            _controller = new AuthController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockConfiguration.Object,
                _mockLogger.Object);

            // Setup controller context for get-my-token endpoint
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "user1"),
                new Claim(ClaimTypes.Name, "user1@example.com"),
                new Claim(ClaimTypes.Role, "Employee")
            }, "Identity.Application"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task GetToken_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user1@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _controller.GetToken(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tokenResponse = Assert.IsType<TokenResponseDto>(okResult.Value);
            Assert.NotEmpty(tokenResponse.Token);
            Assert.True(tokenResponse.Expiration > DateTime.UtcNow);

            // Verify JWT structure
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenResponse.Token);
            Assert.Equal("user1@example.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Employee");
        }

        [Fact]
        public async Task GetToken_WithInvalidEmail_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "nonexistent@example.com",
                Password = "Password123!"
            };

            // Act
            var result = await _controller.GetToken(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid credentials.", ((dynamic)unauthorizedResult.Value).message);
        }

        [Fact]
        public async Task GetToken_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user1@example.com",
                Password = "WrongPassword"
            };

            // Act
            var result = await _controller.GetToken(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid credentials.", ((dynamic)unauthorizedResult.Value).message);
        }

        [Fact]
        public async Task GetToken_WithLockedOutUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user1@example.com",
                Password = "Password123!"
            };

            // Mock locked-out user
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _controller.GetToken(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Account locked out.", ((dynamic)unauthorizedResult.Value).message);
        }

        [Fact]
        public async Task GetToken_WithNotAllowedUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user1@example.com",
                Password = "Password123!"
            };

            // Mock not-allowed user
            _mockSignInManager.Setup(m => m.CheckPasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.NotAllowed);

            // Act
            var result = await _controller.GetToken(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Login not allowed.", ((dynamic)unauthorizedResult.Value).message);
        }

        [Fact]
        public async Task GetToken_WithMissingJwtConfiguration_ReturnsInternalServerError()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user1@example.com",
                Password = "Password123!"
            };

            // Mock missing JWT configuration
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns((string)null);

            // Act
            var result = await _controller.GetToken(loginDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error generating token response.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetToken_WithException_ReturnsInternalServerError()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Email = "user1@example.com",
                Password = "Password123!"
            };

            // Mock exception during JWT generation
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetToken(loginDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("An unexpected error occurred during authentication.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetMyToken_WithAuthenticatedUser_ReturnsOkWithToken()
        {
            // Arrange - Controller context is already set up in constructor

            // Act
            var result = await _controller.GetMyToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var tokenResponse = Assert.IsType<TokenResponseDto>(okResult.Value);
            Assert.NotEmpty(tokenResponse.Token);
            Assert.True(tokenResponse.Expiration > DateTime.UtcNow);

            // Verify JWT structure
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(tokenResponse.Token);
            Assert.Equal("user1@example.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.Contains(token.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Employee");
        }

        [Fact]
        public async Task GetMyToken_WithNonExistentUser_ReturnsUnauthorized()
        {
            // Arrange - Setup GetUserAsync to return null
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((User)null);

            // Act
            var result = await _controller.GetMyToken();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User session not found or invalid.", ((dynamic)unauthorizedResult.Value).message);
        }

        [Fact]
        public async Task GetMyToken_WithMissingJwtConfiguration_ReturnsInternalServerError()
        {
            // Arrange - Mock missing JWT configuration
            _mockConfiguration.Setup(c => c["Jwt:Key"]).Returns((string)null);

            // Act
            var result = await _controller.GetMyToken();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error generating token response.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetMyToken_WithException_ReturnsInternalServerError()
        {
            // Arrange - Mock exception during JWT generation
            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetMyToken();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);
            Assert.Equal("Error generating token.", statusCodeResult.Value);
        }
    }
}
