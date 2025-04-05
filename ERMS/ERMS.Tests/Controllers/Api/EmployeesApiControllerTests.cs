using Xunit;
using Moq;
using ERMS.Controllers.Api; // Controller namespace
using ERMS.Data;
using ERMS.Models;
using ERMS.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq.EntityFrameworkCore; // For DbSets mocking
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace ERMS.Tests.Controllers.Api
{
    public class EmployeesApiControllerTests
    {
        // --- Mocks ---
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
        private readonly Mock<ILogger<EmployeesApiController>> _mockLogger;
        private readonly EmployeesApiController _controller;

        // --- Test Data ---
        private readonly List<User> _testUsers;
        private readonly List<IdentityRole> _testRoles;
        private readonly List<Department> _testDepartments;
        private readonly List<Project> _testProjects;
        private readonly List<ProjectTask> _testTasks;

        // --- Helper to create UserManager Mock ---
        private Mock<UserManager<User>> CreateMockUserManager(List<User> usersToUse)
        {
            var store = new Mock<IUserEmailStore<User>>(); // Include email methods
            store.As<IUserRoleStore<User>>(); // And role methods
            var userManagerMock = new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);

            // Mock store methods using the PASSED IN list
            store.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                  .Returns<string, CancellationToken>((id, cancelToken) => Task.FromResult(usersToUse.FirstOrDefault(u => u.Id == id)));

            store.Setup(s => s.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .Returns<string, CancellationToken>((normalizedEmail, cancelToken) => Task.FromResult(usersToUse.FirstOrDefault(u => u.NormalizedEmail == normalizedEmail)));

            store.Setup(s => s.GetEmailAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                  .Returns<User, CancellationToken>((u, c) => Task.FromResult(u?.Email)); // Handle null user

            store.Setup(s => s.GetUserNameAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                  .Returns<User, CancellationToken>((u, c) => Task.FromResult(u?.UserName));

            // Mock direct UserManager methods called by controller
            userManagerMock.Setup(u => u.FindByIdAsync(It.IsAny<string>()))
                 .Returns<string>(id => Task.FromResult(usersToUse.FirstOrDefault(u => u.Id == id))); // Needed by Update/Delete

            userManagerMock.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
                .Returns<string>(email => Task.FromResult(usersToUse.FirstOrDefault(u => u.NormalizedEmail == email?.ToUpperInvariant()))); // Needed by Update check

            // Mock user creation - Setup specific return results in tests
            userManagerMock.Setup(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                           .ReturnsAsync(IdentityResult.Success); // Default success

            // Mock user update - Setup specific return results in tests
            userManagerMock.Setup(u => u.UpdateAsync(It.IsAny<User>()))
                           .ReturnsAsync(IdentityResult.Success); // Default success

            // Mock email/username setting
            userManagerMock.Setup(u => u.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(u => u.SetUserNameAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            // Mock user deletion - Setup specific return results in tests
            userManagerMock.Setup(u => u.DeleteAsync(It.IsAny<User>()))
                           .ReturnsAsync(IdentityResult.Success); // Default success

            // Mock role methods - Setup specific return results in tests
            userManagerMock.Setup(u => u.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string>()); // Default empty list
            userManagerMock.Setup(u => u.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(u => u.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);


            return userManagerMock;
        }

        // --- Helper to create RoleManager Mock ---
        private Mock<RoleManager<IdentityRole>> CreateMockRoleManager(List<IdentityRole> rolesToUse)
        {
            var store = new Mock<IRoleStore<IdentityRole>>();
            var roleManagerMock = new Mock<RoleManager<IdentityRole>>(store.Object, null, null, null, null);

            // Mock RoleExistsAsync needed by Create/Update actions
            roleManagerMock.Setup(r => r.RoleExistsAsync(It.IsAny<string>()))
                           .Returns<string>(roleName => Task.FromResult(rolesToUse.Any(r => r.Name == roleName)));

            return roleManagerMock;
        }

        // --- Constructor ---
        public EmployeesApiControllerTests()
        {
            // 1. Prepare Test Data
            _testDepartments = new List<Department> {
                 new Department { DepartmentID = 1, DepartmentName = "Sales" },
                 new Department { DepartmentID = 2, DepartmentName = "Support" }
            };
            _testUsers = new List<User> {
                 new User { Id = "user1", FirstName = "Test", LastName = "Admin", Email="admin@test.com", UserName="admin@test.com", NormalizedEmail="ADMIN@TEST.COM", DepartmentID = 1, Department = _testDepartments[0], HireDate=DateTime.UtcNow.AddYears(-1) },
                 new User { Id = "user2", FirstName = "Test", LastName = "Manager", Email="manager@test.com", UserName="manager@test.com", NormalizedEmail="MANAGER@TEST.COM", DepartmentID = 1, Department = _testDepartments[0], HireDate=DateTime.UtcNow.AddMonths(-6) },
                 new User { Id = "user3", FirstName = "Test", LastName = "Employee", Email="emp@test.com", UserName="emp@test.com", NormalizedEmail="EMP@TEST.COM", DepartmentID = 2, Department = _testDepartments[1], HireDate=DateTime.UtcNow.AddDays(-10) }
             };
            _testRoles = new List<IdentityRole> { // Used for RoleManager Mock
                 new IdentityRole { Id = "role1", Name = "Admin", NormalizedName = "ADMIN"},
                 new IdentityRole { Id = "role2", Name = "Manager", NormalizedName = "MANAGER"},
                 new IdentityRole { Id = "role3", Name = "Employee", NormalizedName = "EMPLOYEE"}
             };
            _testProjects = new List<Project> { new Project { ProjectID = 10, ManagerID = "user2" } }; // For delete check
            _testTasks = new List<ProjectTask> { new ProjectTask { TaskID = 100, AssigneeID = "user3" } }; // For delete check


            // 2. Mock DbContext (Needed primarily for Includes and dependency checks)
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
            _mockContext = new Mock<ApplicationDbContext>(options);
            // Setup DbSets using Moq.EntityFrameworkCore (Assuming virtual DbSets)
            _mockContext.Setup(c => c.Users).ReturnsDbSet(_testUsers);
            _mockContext.Setup(c => c.Departments).ReturnsDbSet(_testDepartments);
            _mockContext.Setup(c => c.Projects).ReturnsDbSet(_testProjects);
            _mockContext.Setup(c => c.Tasks).ReturnsDbSet(_testTasks);
            // SaveChanges not directly used by controller but good practice to mock
            _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // 3. Mock Identity Managers
            _mockUserManager = CreateMockUserManager(_testUsers);
            _mockRoleManager = CreateMockRoleManager(_testRoles);

            // 4. Mock Logger
            _mockLogger = new Mock<ILogger<EmployeesApiController>>();

            // 5. Create Controller Instance
            _controller = new EmployeesApiController(
                _mockContext.Object,
                _mockUserManager.Object,
                _mockRoleManager.Object,
                _mockLogger.Object);
        }

        // --- Test Methods ---

        // -- GET Employees --
        [Fact]
        public async Task GetEmployees_ReturnsOkObjectResult_WithListOfEmployeeDtos()
        {
            // Arrange (Uses context mock with Includes)
            // Act
            var result = await _controller.GetEmployees();
            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<EmployeeDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedDtos = Assert.IsAssignableFrom<IEnumerable<EmployeeDto>>(okResult.Value);
            Assert.Equal(3, returnedDtos.Count());
            Assert.Equal("Sales", returnedDtos.First(dto => dto.Id == "user1").DepartmentName);
        }

        // -- GET Employee (By ID) --
        [Fact]
        public async Task GetEmployee_ExistingId_ReturnsOkObjectResult_WithEmployeeDto()
        {
            // Arrange
            string existingUserId = "user3";
            // Act
            var result = await _controller.GetEmployee(existingUserId);
            // Assert
            var actionResult = Assert.IsType<ActionResult<EmployeeDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedDto = Assert.IsType<EmployeeDto>(okResult.Value);
            Assert.Equal(existingUserId, returnedDto.Id);
            Assert.Equal("Employee", returnedDto.LastName);
            Assert.Equal("Support", returnedDto.DepartmentName);
        }

        [Fact]
        public async Task GetEmployee_NonExistingId_ReturnsNotFoundResult()
        {
            // Arrange
            string nonExistingUserId = "user_invalid";
            // Act
            var result = await _controller.GetEmployee(nonExistingUserId);
            // Assert
            var actionResult = Assert.IsType<ActionResult<EmployeeDto>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result); // API returns NotFoundObjectResult
        }

        [Fact]
        public async Task GetEmployee_NullOrEmptyId_ReturnsBadRequest()
        {
            // Act
            var resultNull = await _controller.GetEmployee(null!);
            var resultEmpty = await _controller.GetEmployee(string.Empty);
            // Assert
            Assert.IsType<BadRequestObjectResult>(resultNull.Result);
            Assert.IsType<BadRequestObjectResult>(resultEmpty.Result);
        }

        // -- POST CreateEmployee --
        [Fact]
        public async Task CreateEmployee_ValidData_ReturnsCreatedAtAction()
        {
            // Arrange
            var createDto = new CreateEmployeeDto { FirstName = "New", LastName = "Dev", Email = "newdev@test.com", Password = "ValidPassword1!", HireDate = DateTime.UtcNow, DepartmentID = 1, Roles = new List<string> { "Employee" } };
            var createdUser = new User { Id = "newUserCreated" }; // Simulate ID generation on callback
                                                                  // Mock sequence: Dept exists, Role Exists, CreateAsync SUCceeds, AddToRolesAsync Succeeds, Find Dept after
            _mockContext.Setup(c => c.Departments.AnyAsync(d => d.DepartmentID == createDto.DepartmentID, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockRoleManager.Setup(r => r.RoleExistsAsync("Employee")).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<User>(), createDto.Password)).ReturnsAsync(IdentityResult.Success).Callback<User, string>((u, p) => u.Id = createdUser.Id); // Simulate setting ID
            _mockUserManager.Setup(u => u.AddToRolesAsync(It.Is<User>(usr => usr.Id == createdUser.Id), createDto.Roles)).ReturnsAsync(IdentityResult.Success);
            _mockContext.Setup(c => c.Departments.FindAsync(createDto.DepartmentID)).ReturnsAsync(_testDepartments.First(d => d.DepartmentID == createDto.DepartmentID));


            // Act
            var result = await _controller.CreateEmployee(createDto);

            // Assert
            var actionResult = Assert.IsType<ActionResult<EmployeeDto>>(result);
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
            var returnedDto = Assert.IsType<EmployeeDto>(createdAtActionResult.Value);

            Assert.Equal(createDto.Email, returnedDto.Email);
            Assert.Equal(createdUser.Id, returnedDto.Id);
            Assert.Equal("Sales", returnedDto.DepartmentName);
            Assert.Equal(nameof(_controller.GetEmployee), createdAtActionResult.ActionName);

            _mockUserManager.Verify(u => u.CreateAsync(It.Is<User>(usr => usr.Email == createDto.Email), createDto.Password), Times.Once);
            _mockUserManager.Verify(u => u.AddToRolesAsync(It.Is<User>(usr => usr.Id == createdUser.Id), createDto.Roles), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_InvalidDepartment_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateEmployeeDto { DepartmentID = 999, /* other required fields */ Email = "t1@t.com", Password = "p", FirstName = "f", LastName = "l", HireDate = DateTime.UtcNow, Roles = { "Employee" } };
            _mockContext.Setup(c => c.Departments.AnyAsync(d => d.DepartmentID == 999, It.IsAny<CancellationToken>())).ReturnsAsync(false); // Dept check fails
                                                                                                                                            // Act
            var result = await _controller.CreateEmployee(createDto);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockUserManager.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateEmployee_InvalidRole_ReturnsBadRequest()
        {
            // Arrange
            var createDto = new CreateEmployeeDto { Roles = new List<string> { "InvalidRole" }, DepartmentID = 1, /* other required fields */ Email = "t1@t.com", Password = "p", FirstName = "f", LastName = "l", HireDate = DateTime.UtcNow };
            _mockContext.Setup(c => c.Departments.AnyAsync(d => d.DepartmentID == 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockRoleManager.Setup(r => r.RoleExistsAsync("InvalidRole")).ReturnsAsync(false); // Role check fails
                                                                                               // Act
            var result = await _controller.CreateEmployee(createDto);
            // Assert
            Assert.IsType<BadRequestObjectResult>(result.Result);
            _mockUserManager.Verify(u => u.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateEmployee_CreateAsyncFails_ReturnsBadRequestWithErrors()
        {
            // Arrange
            var createDto = new CreateEmployeeDto { DepartmentID = 1, Email = "t1@t.com", Password = "p", FirstName = "f", LastName = "l", HireDate = DateTime.UtcNow, Roles = { "Employee" } };
            var errors = new List<IdentityError> { new IdentityError { Code = "Fail", Description = "Create failed" } };
            _mockContext.Setup(c => c.Departments.AnyAsync(d => d.DepartmentID == 1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            _mockRoleManager.Setup(r => r.RoleExistsAsync("Employee")).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.CreateAsync(It.IsAny<User>(), createDto.Password)).ReturnsAsync(IdentityResult.Failed(errors.ToArray())); // CreateAsync fails
                                                                                                                                                    // Act
            var result = await _controller.CreateEmployee(createDto);
            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.True(((ValidationProblemDetails)badRequestResult.Value!).Errors.ContainsKey(string.Empty)); // Check model state has general error
            _mockUserManager.Verify(u => u.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        }

        // --- PUT UpdateEmployee ---
        // TODO: Add tests for UpdateEmployee (Success, NotFound, Validation Failures, Role Update logic)

        // --- DELETE DeleteEmployee ---
        [Fact]
        public async Task DeleteEmployee_ExistingId_NoDependencies_ReturnsNoContent()
        {
            // Arrange
            string userIdToDelete = "user1"; // Assume no dependencies in test data
            var userToDelete = _testUsers.First(u => u.Id == userIdToDelete);
            _mockUserManager.Setup(u => u.FindByIdAsync(userIdToDelete)).ReturnsAsync(userToDelete);
            _mockContext.Setup(c => c.Projects.AnyAsync(p => p.ManagerID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(false); // No projects managed
            _mockContext.Setup(c => c.Tasks.AnyAsync(t => t.AssigneeID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(false); // No tasks assigned
            _mockUserManager.Setup(u => u.DeleteAsync(userToDelete)).ReturnsAsync(IdentityResult.Success); // Delete succeeds

            // Act
            var result = await _controller.DeleteEmployee(userIdToDelete);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _mockUserManager.Verify(u => u.DeleteAsync(userToDelete), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployee_ExistingId_WithProjectDependency_ReturnsBadRequest()
        {
            // Arrange
            string userIdToDelete = "user2"; // User 2 manages project 10
            var userToDelete = _testUsers.First(u => u.Id == userIdToDelete);
            _mockUserManager.Setup(u => u.FindByIdAsync(userIdToDelete)).ReturnsAsync(userToDelete);
            _mockContext.Setup(c => c.Projects.AnyAsync(p => p.ManagerID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(true); // Has project!
            _mockContext.Setup(c => c.Tasks.AnyAsync(t => t.AssigneeID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteEmployee(userIdToDelete);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result); // Check it's BadRequest
            _mockUserManager.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployee_ExistingId_WithTaskDependency_ReturnsBadRequest()
        {
            // Arrange
            string userIdToDelete = "user3"; // User 3 has task 100 assigned
            var userToDelete = _testUsers.First(u => u.Id == userIdToDelete);
            _mockUserManager.Setup(u => u.FindByIdAsync(userIdToDelete)).ReturnsAsync(userToDelete);
            _mockContext.Setup(c => c.Projects.AnyAsync(p => p.ManagerID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockContext.Setup(c => c.Tasks.AnyAsync(t => t.AssigneeID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(true); // Has Task!

            // Act
            var result = await _controller.DeleteEmployee(userIdToDelete);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            _mockUserManager.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployee_NonExistingId_ReturnsNotFound()
        {
            // Arrange
            string nonExistingUserId = "user_invalid";
            _mockUserManager.Setup(u => u.FindByIdAsync(nonExistingUserId)).ReturnsAsync((User?)null); // Find returns null

            // Act
            var result = await _controller.DeleteEmployee(nonExistingUserId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
            _mockUserManager.Verify(u => u.DeleteAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployee_DeleteAsyncFails_ReturnsInternalServerError()
        {
            // Arrange
            string userIdToDelete = "user1";
            var userToDelete = _testUsers.First(u => u.Id == userIdToDelete);
            var errors = new List<IdentityError> { new IdentityError { Code = "DelFail", Description = "Delete failed" } };
            _mockUserManager.Setup(u => u.FindByIdAsync(userIdToDelete)).ReturnsAsync(userToDelete);
            _mockContext.Setup(c => c.Projects.AnyAsync(p => p.ManagerID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockContext.Setup(c => c.Tasks.AnyAsync(t => t.AssigneeID == userIdToDelete, It.IsAny<CancellationToken>())).ReturnsAsync(false);
            _mockUserManager.Setup(u => u.DeleteAsync(userToDelete)).ReturnsAsync(IdentityResult.Failed(errors.ToArray())); // DeleteAsync fails

            // Act
            var result = await _controller.DeleteEmployee(userIdToDelete);

            // Assert
            var statusCodeResult = Assert.IsAssignableFrom<ObjectResult>(result);
            // Controller returns StatusCode(500,...) or BadRequest(ModelState) depending on implementation detail
            Assert.True(statusCodeResult.StatusCode == StatusCodes.Status500InternalServerError || statusCodeResult.StatusCode == StatusCodes.Status400BadRequest);
        }

        // --- TODO: Add tests for UpdateEmployee ---

    }
}