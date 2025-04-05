using Xunit;
using Moq;
using ERMS.Controllers;
using ERMS.Data;
using ERMS.Models;
using ERMS.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Moq.EntityFrameworkCore;

namespace ERMS.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly AdminController _controller;
        private readonly List<User> _testUsers;
        private readonly List<Department> _testDepartments;
        private readonly List<IdentityRole> _testRoles;

        public AdminControllerTests()
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
                    FirstName = "Regular",
                    LastName = "User",
                    DepartmentID = 1,
                    Department = _testDepartments[0]
                },
                new User
                {
                    Id = "user2",
                    UserName = "admin@example.com",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    DepartmentID = 2,
                    Department = _testDepartments[1],
                    LockoutEnd = System.DateTimeOffset.Now.AddDays(1) // Locked user
                }
            };

            _testRoles = new List<IdentityRole>
            {
                new IdentityRole { Id = "role1", Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = "role2", Name = "Manager", NormalizedName = "MANAGER" },
                new IdentityRole { Id = "role3", Name = "Employee", NormalizedName = "EMPLOYEE" }
            };

            // Setup DbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"AdminTestDb_{System.Guid.NewGuid()}")
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
            _mockUserManager.Setup(m => m.Users)
                .Returns(_testUsers.AsQueryable());

            _mockUserManager.Setup(m => m.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _testUsers.FirstOrDefault(u => u.Id == id));

            _mockUserManager.Setup(m => m.GetRolesAsync(It.IsAny<User>()))
                .ReturnsAsync((User user) =>
                {
                    if (user.Id == "user1") return new List<string> { "Employee" };
                    if (user.Id == "user2") return new List<string> { "Admin", "Manager" };
                    return new List<string>();
                });

            _mockUserManager.Setup(m => m.IsInRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ReturnsAsync((User user, string role) =>
                {
                    if (user.Id == "user1" && role == "Employee") return true;
                    if (user.Id == "user2" && (role == "Admin" || role == "Manager")) return true;
                    return false;
                });

            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager.Setup(m => m.SetLockoutEndDateAsync(It.IsAny<User>(), It.IsAny<System.DateTimeOffset?>()))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<User, System.DateTimeOffset?>((user, lockoutEnd) =>
                {
                    var userToUpdate = _testUsers.FirstOrDefault(u => u.Id == user.Id);
                    if (userToUpdate != null)
                    {
                        userToUpdate.LockoutEnd = lockoutEnd;
                    }
                });

            // Setup RoleManager
            var roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
            _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
                roleStoreMock.Object, null, null, null, null);

            _mockRoleManager.Setup(m => m.Roles)
                .Returns(_testRoles.AsQueryable());

            // Setup TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Create controller
            _controller = new AdminController(
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockContext.Object)
            {
                TempData = tempData,
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.Name, "admin@example.com"),
                            new Claim(ClaimTypes.NameIdentifier, "user2"),
                            new Claim(ClaimTypes.Role, "Admin")
                        }, "mock"))
                    }
                }
            };
        }

        [Fact]
        public async Task Users_ReturnsViewResult_WithUserManagementViewModels()
        {
            // Act
            var result = await _controller.Users();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<UserManagementViewModel>>(viewResult.Model);
            Assert.Equal(2, model.Count());

            // Verify first user data
            var firstUser = model.First();
            Assert.Equal("user1", firstUser.UserId);
            Assert.Equal("user1@example.com", firstUser.Email);
            Assert.Equal("Regular", firstUser.FirstName);
            Assert.Equal("User", firstUser.LastName);
            Assert.Equal("Human Resources", firstUser.Department);
            Assert.Contains("Employee", firstUser.Roles);
            Assert.False(firstUser.IsLocked);

            // Verify second user is locked
            var secondUser = model.Last();
            Assert.True(secondUser.IsLocked);
        }

        [Fact]
        public async Task EditUserRoles_Get_WithValidId_ReturnsViewResult_WithModel()
        {
            // Arrange
            string validId = "user1";

            // Act
            var result = await _controller.EditUserRoles(validId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<EditUserRolesViewModel>(viewResult.Model);
            Assert.Equal(validId, model.UserId);
            Assert.Equal("user1@example.com", model.Email);
            Assert.Equal("Regular User", model.UserName);
            Assert.Equal(3, model.Roles.Count);
            Assert.True(model.Roles.Any(r => r.RoleName == "Employee" && r.IsSelected));
        }

        [Fact]
        public async Task EditUserRoles_Get_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            string invalidId = "nonexistentuser";

            // Act
            var result = await _controller.EditUserRoles(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditUserRoles_Get_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.EditUserRoles("null");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditUserRoles_Post_WithValidModel_RedirectsToUsers()
        {
            // Arrange
            var model = new EditUserRolesViewModel
            {
                UserId = "user1",
                Email = "user1@example.com",
                UserName = "Regular User",
                Roles = new List<RoleSelection>
                {
                    new RoleSelection { RoleName = "Admin", IsSelected = true },
                    new RoleSelection { RoleName = "Manager", IsSelected = false },
                    new RoleSelection { RoleName = "Employee", IsSelected = true }
                }
            };

            // Act
            var result = await _controller.EditUserRoles(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirectResult.ActionName);

            // Verify roles were removed and added
            _mockUserManager.Verify(m => m.RemoveFromRolesAsync(
                It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Once);
            _mockUserManager.Verify(m => m.AddToRolesAsync(
                It.IsAny<User>(), It.Is<IEnumerable<string>>(roles =>
                    roles.Contains("Admin") && roles.Contains("Employee"))), Times.Once);
        }

        [Fact]
        public async Task EditUserRoles_Post_WithInvalidUserId_ReturnsNotFound()
        {
            // Arrange
            var model = new EditUserRolesViewModel
            {
                UserId = "nonexistentuser",
                Roles = new List<RoleSelection>()
            };

            // Act
            var result = await _controller.EditUserRoles(model);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditUserRoles_Post_RemoveRolesFails_ReturnsViewWithError()
        {
            // Arrange
            var model = new EditUserRolesViewModel
            {
                UserId = "user1",
                Roles = new List<RoleSelection>
                {
                    new RoleSelection { RoleName = "Admin", IsSelected = true }
                }
            };

            _mockUserManager.Setup(m => m.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Cannot remove roles" }));

            // Act
            var result = await _controller.EditUserRoles(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<EditUserRolesViewModel>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task EditUserRoles_Post_AddRolesFails_ReturnsViewWithError()
        {
            // Arrange
            var model = new EditUserRolesViewModel
            {
                UserId = "user1",
                Roles = new List<RoleSelection>
                {
                    new RoleSelection { RoleName = "Admin", IsSelected = true }
                }
            };

            _mockUserManager.Setup(m => m.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Cannot add roles" }));

            // Act
            var result = await _controller.EditUserRoles(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<EditUserRolesViewModel>(viewResult.Model);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public async Task ToggleUserLock_WithLockedUser_UnlocksUser()
        {
            // Arrange
            string lockedUserId = "user2"; // This user is locked in the test data

            // Act
            var result = await _controller.ToggleUserLock(lockedUserId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirectResult.ActionName);

            // Verify the user was unlocked
            _mockUserManager.Verify(m => m.SetLockoutEndDateAsync(
                It.Is<User>(u => u.Id == lockedUserId),
                It.Is<System.DateTimeOffset?>(d => d == null)), Times.Once);
        }

        [Fact]
        public async Task ToggleUserLock_WithUnlockedUser_LocksUser()
        {
            // Arrange
            string unlockedUserId = "user1"; // This user is not locked in the test data

            // Act
            var result = await _controller.ToggleUserLock(unlockedUserId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirectResult.ActionName);

            // Verify the user was locked
            _mockUserManager.Verify(m => m.SetLockoutEndDateAsync(
                It.Is<User>(u => u.Id == unlockedUserId),
                It.IsAny<System.DateTimeOffset?>()), Times.Once);
        }

        [Fact]
        public async Task ToggleUserLock_WithInvalidUserId_ReturnsNotFound()
        {
            // Arrange
            string invalidUserId = "nonexistentuser";

            // Act
            var result = await _controller.ToggleUserLock(invalidUserId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
