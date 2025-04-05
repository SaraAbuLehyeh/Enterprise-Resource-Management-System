using Xunit;
using Moq;
using ERMS.Controllers;
using ERMS.Data;
using ERMS.Models;
using ERMS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Moq.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERMS.Tests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<SignInManager<User>> _mockSignInManager;
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<ILogger<AccountController>> _mockLogger;
        private readonly AccountController _controller;
        private readonly List<User> _testUsers;
        private readonly List<Department> _testDepartments;

        public AccountControllerTests()
        {
            // Setup test data
            _testDepartments = new List<Department>
            {
                new Department { DepartmentID = 1, DepartmentName = "Human Resources" },
                new Department { DepartmentID = 2, DepartmentName = "Information Technology" }
            };

            _testUsers = new List<User>
            {
                new User
                {
                    Id = "user1",
                    UserName = "user1@example.com",
                    Email = "user1@example.com",
                    FirstName = "Test",
                    LastName = "User",
                    DepartmentID = 1
                },
                new User
                {
                    Id = "user2",
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    DepartmentID = 2
                }
            };

            // Setup DbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"AccountTestDb_{System.Guid.NewGuid()}")
                .Options;
            _mockContext = new Mock<ApplicationDbContext>(options);

            // Setup mock DbSets
            _mockContext.Setup(c => c.Users).ReturnsDbSet(_testUsers);
            _mockContext.Setup(c => c.Departments).ReturnsDbSet(_testDepartments);

            // Setup UserManager
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            // Setup UserManager methods
            _mockUserManager.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((string email) => _testUsers.FirstOrDefault(u => u.Email == email));

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            // Setup SignInManager
            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                Mock.Of<IHttpContextAccessor>(),
                Mock.Of<IUserClaimsPrincipalFactory<User>>(),
                null, null, null, null);

            // Setup SignInManager methods
            _mockSignInManager.Setup(m => m.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Setup Logger
            _mockLogger = new Mock<ILogger<AccountController>>();

            // Setup TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Create controller
            _controller = new AccountController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                _mockContext.Object,
                _mockLogger.Object)
            {
                TempData = tempData,
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public void Login_Get_ReturnsViewResult()
        {
            // Act
            var result = _controller.Login();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.Model);
        }

        [Fact]
        public async Task Login_Post_WithValidModel_RedirectsToReturnUrl()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "user1@example.com",
                Password = "Password123!",
                RememberMe = false
            };
            var returnUrl = "/Dashboard";

            // Act
            var result = await _controller.Login(model, returnUrl);

            // Assert
            var redirectResult = Assert.IsType<LocalRedirectResult>(result);
            Assert.Equal(returnUrl, redirectResult.Url);

            // Verify SignInManager was called
            _mockSignInManager.Verify(m => m.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task Login_Post_WithInvalidCredentials_ReturnsViewWithModel()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "invalid@example.com",
                Password = "WrongPassword",
                RememberMe = false
            };

            _mockSignInManager.Setup(m => m.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<LoginViewModel>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Login_Post_WithLockedOutAccount_RedirectsToLockout()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "locked@example.com",
                Password = "Password123!",
                RememberMe = false
            };

            _mockSignInManager.Setup(m => m.PasswordSignInAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _controller.Login(model, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Lockout", redirectResult.ActionName);
        }

       

        [Fact]
        public async Task Register_Post_WithValidModel_RedirectsToHome()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FirstName = "New",
                LastName = "User",
                HireDate = System.DateTime.Now,
                DepartmentID = 1
            };

            _mockContext.Setup(c => c.Departments.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Department, bool>>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Register(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // Verify UserManager was called to create user and assign role
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), model.Password), Times.Once);
            _mockUserManager.Verify(m => m.AddToRoleAsync(It.IsAny<User>(), "Employee"), Times.Once);

            // Verify user was signed in
            _mockSignInManager.Verify(m => m.SignInAsync(It.IsAny<User>(), false, null), Times.Once);
        }

        [Fact]
        public async Task Register_Post_WithInvalidDepartment_ReturnsViewWithError()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Email = "newuser@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FirstName = "New",
                LastName = "User",
                HireDate = System.DateTime.Now,
                DepartmentID = 999 // Invalid department ID
            };

            _mockContext.Setup(c => c.Departments.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<Department, bool>>>(),
                It.IsAny<System.Threading.CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<RegisterViewModel>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);

            // Verify UserManager was NOT called to create user
            _mockUserManager.Verify(m => m.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Logout_RedirectsToHome()
        {
            // Arrange - set up a user identity in the HttpContext
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "user1@example.com"),
                new Claim(ClaimTypes.NameIdentifier, "user1")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _controller.ControllerContext.HttpContext.User = claimsPrincipal;

            // Act
            var result = await _controller.Logout();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Home", redirectResult.ControllerName);

            // Verify SignInManager was called
            _mockSignInManager.Verify(m => m.SignOutAsync(), Times.Once);
        }

        [Fact]
        public void Lockout_ReturnsViewResult()
        {
            // Act
            var result = _controller.Lockout();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void AccessDenied_ReturnsViewResult()
        {
            // Act
            var result = _controller.AccessDenied();

            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }
}
