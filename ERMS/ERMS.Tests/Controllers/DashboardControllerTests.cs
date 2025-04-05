using Xunit;
using Moq;
using ERMS.Controllers; // Controller namespace
using ERMS.Data;
using ERMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq.EntityFrameworkCore; // For ReturnsDbSet
using System.Security.Claims; // For ClaimsPrincipal
using Microsoft.AspNetCore.Http; // For HttpContext

namespace ERMS.Tests.Controllers
{
    public class DashboardControllerTests
    {
        // --- Mocks ---
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly DashboardController _controller;

        // --- Test Data ---
        private readonly List<ProjectTask> _testTasks;
        private readonly List<Project> _testProjects;
        private readonly List<User> _testUsers;

        // Helper method to setup Controller Context with a logged-in user
        private void SetupUserContext(string userId, string userEmail, string[] roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId), // User ID claim
                new Claim(ClaimTypes.Name, userEmail)       // User Name claim (used by User.Identity.Name)
            };
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role)); // Add role claims
            }

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }


        // --- Constructor (Common Setup) ---
        public DashboardControllerTests()
        {
            // 1. Prepare Test Data
            _testUsers = new List<User> {
                new User { Id = "user1", FirstName = "Regular", LastName = "Employee", Email = "emp@test.com"},
                new User { Id = "user2", FirstName = "Test", LastName = "Manager", Email = "manager@test.com"},
                new User { Id = "user3", FirstName = "Test", LastName = "Admin", Email = "admin@test.com"}
            };
            _testProjects = new List<Project> {
                new Project { ProjectID = 1, ProjectName = "Alpha" },
                new Project { ProjectID = 2, ProjectName = "Beta", Tasks = new List<ProjectTask>() } // Project with no tasks initially
            };
            _testTasks = new List<ProjectTask> {
                // Tasks for Employee (user1)
                new ProjectTask { TaskID = 101, TaskName = "User1 Task Done", ProjectID = 1, Project = _testProjects[0], AssigneeID = "user1", Assignee = _testUsers[0], DueDate = DateTime.UtcNow.AddDays(10), Status="Completed", Priority="Low" },
                new ProjectTask { TaskID = 102, TaskName = "User1 Task Progress", ProjectID = 1, Project = _testProjects[0], AssigneeID = "user1", Assignee = _testUsers[0], DueDate = DateTime.UtcNow.AddDays(5), Status="In Progress", Priority="Med" }, // Upcoming
                new ProjectTask { TaskID = 103, TaskName = "User1 Task Future", ProjectID = 1, Project = _testProjects[0], AssigneeID = "user1", Assignee = _testUsers[0], DueDate = DateTime.UtcNow.AddDays(15), Status="Not Started", Priority="Med" }, // Future
                // Task for Manager (user2)
                new ProjectTask { TaskID = 104, TaskName = "User2 Task Due Soon", ProjectID = 2, Project = _testProjects[1], AssigneeID = "user2", Assignee = _testUsers[1], DueDate = DateTime.UtcNow.AddDays(2), Status="Not Started", Priority="High" } // Upcoming
                // User3 (Admin) has no tasks assigned
            };
            // Link tasks back to projects
            _testProjects[0].Tasks = new List<ProjectTask> { _testTasks[0], _testTasks[1], _testTasks[2] };
            _testProjects[1].Tasks.Add(_testTasks[3]);


            // 2. Mock DbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().Options;
            _mockContext = new Mock<ApplicationDbContext>(options);

            // 3. Setup Mock DbContext Behavior
            _mockContext.Setup(c => c.Tasks).ReturnsDbSet(_testTasks);
            _mockContext.Setup(c => c.Projects).ReturnsDbSet(_testProjects);
            _mockContext.Setup(c => c.Users).ReturnsDbSet(_testUsers);

            // 4. Create Controller Instance (HttpContext set per-test)
            _controller = new DashboardController(_mockContext.Object);
        }

        // --- Test Methods ---

        [Fact]
        public async Task Index_ForEmployee_ReturnsViewResult_WithCorrectTaskCountsAndUpcoming()
        {
            // Arrange
            string employeeId = "user1";
            string employeeEmail = "emp@test.com";
            SetupUserContext(employeeId, employeeEmail, new[] { "Employee" }); // Simulate Employee login

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);

            // Check task counts for user1
            Assert.Equal(3, viewResult.ViewData["TotalTasks"]);
            Assert.Equal(1, viewResult.ViewData["CompletedTasks"]);
            Assert.Equal(1, viewResult.ViewData["InProgressTasks"]);
            Assert.Equal(1, viewResult.ViewData["PendingTasks"]);

            // Check upcoming deadlines (Task 102 is due in 5 days)
            var upcoming = Assert.IsAssignableFrom<List<ProjectTask>>(viewResult.ViewData["UpcomingDeadlines"]);
            Assert.Single(upcoming);
            Assert.Equal(102, upcoming[0].TaskID);

            // Check project stats are NOT set for employee
            Assert.False(viewResult.ViewData.ContainsKey("TotalProjects"));
            Assert.False(viewResult.ViewData.ContainsKey("Projects"));
        }

        [Fact]
        public async Task Index_ForManager_ReturnsViewResult_WithTaskAndProjectCounts()
        {
            // Arrange
            string managerId = "user2";
            string managerEmail = "manager@test.com";
            SetupUserContext(managerId, managerEmail, new[] { "Manager" }); // Simulate Manager login

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);

            // Check task counts for user2 (should have 1 task)
            Assert.Equal(1, viewResult.ViewData["TotalTasks"]);
            Assert.Equal(0, viewResult.ViewData["CompletedTasks"]);
            Assert.Equal(0, viewResult.ViewData["InProgressTasks"]);
            Assert.Equal(1, viewResult.ViewData["PendingTasks"]);

            // Check upcoming deadlines (Task 104 is due in 2 days)
            var upcoming = Assert.IsAssignableFrom<List<ProjectTask>>(viewResult.ViewData["UpcomingDeadlines"]);
            Assert.Single(upcoming);
            Assert.Equal(104, upcoming[0].TaskID);

            // Check project stats ARE set for manager
            Assert.True(viewResult.ViewData.ContainsKey("TotalProjects"));
            Assert.True(viewResult.ViewData.ContainsKey("Projects"));
            Assert.Equal(2, viewResult.ViewData["TotalProjects"]); // Total projects in system
            Assert.Equal(2, ((List<Project>)viewResult.ViewData["Projects"]!).Count);
            Assert.Equal(2, viewResult.ViewData["ProjectsWithTasks"]); // Both projects have tasks linked
        }

        [Fact]
        public async Task Index_ForAdmin_NoTasksAssigned_ReturnsViewResult_WithZeroTaskCountsAndProjectStats()
        {
            // Arrange
            string adminId = "user3";
            string adminEmail = "admin@test.com";
            SetupUserContext(adminId, adminEmail, new[] { "Admin" }); // Simulate Admin login

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);

            // Check task counts for user3 (should be 0)
            Assert.Equal(0, viewResult.ViewData["TotalTasks"]);
            Assert.Equal(0, viewResult.ViewData["CompletedTasks"]);
            Assert.Equal(0, viewResult.ViewData["InProgressTasks"]);
            Assert.Equal(0, viewResult.ViewData["PendingTasks"]);

            // Check upcoming deadlines (should be empty list)
            var upcoming = Assert.IsAssignableFrom<List<ProjectTask>>(viewResult.ViewData["UpcomingDeadlines"]);
            Assert.Empty(upcoming);

            // Check project stats ARE set for admin
            Assert.True(viewResult.ViewData.ContainsKey("TotalProjects"));
            Assert.Equal(2, viewResult.ViewData["TotalProjects"]);
            Assert.Equal(2, viewResult.ViewData["ProjectsWithTasks"]);
        }

        [Fact]
        public async Task Index_UserNotFoundInDb_ReturnsViewResult_WithZeroTaskCounts()
        {
            // Arrange
            // Simulate a user principal where the email doesn't match anyone in _testUsers
            string unknownUserId = "user_unknown";
            string unknownEmail = "unknown@test.com";
            SetupUserContext(unknownUserId, unknownEmail, new[] { "Employee" });

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData);

            // Check task counts (should be 0 as user ID won't match any tasks)
            Assert.Equal(0, viewResult.ViewData["TotalTasks"]);
            Assert.Equal(0, viewResult.ViewData["CompletedTasks"]);
            // ... other task counts ...

            // Check upcoming deadlines (should be empty list)
            var upcoming = Assert.IsAssignableFrom<List<ProjectTask>>(viewResult.ViewData["UpcomingDeadlines"]);
            Assert.Empty(upcoming);

            // Project stats should NOT be set (unless this unknown user had Admin/Manager role)
            Assert.False(viewResult.ViewData.ContainsKey("TotalProjects"));
        }
    }
}