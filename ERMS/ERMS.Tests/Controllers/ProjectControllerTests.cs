using Xunit;
using Moq;
using ERMS.Controllers;
using ERMS.Data;
using ERMS.Models;
using ERMS.DTOs;
using ERMS.HttpClients;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
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
    public class ProjectControllerTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<ProjectApiClient> _mockProjectApiClient;
        private readonly ProjectController _controller;
        private readonly List<Project> _testProjects;
        private readonly List<User> _testUsers;
        private readonly List<ProjectTask> _testTasks;
        private readonly List<ProjectDto> _testProjectDtos;

        public ProjectControllerTests()
        {
            // Setup DbContext options
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"ProjectTestDb_{System.Guid.NewGuid()}")
                .Options;
            _mockContext = new Mock<ApplicationDbContext>(options);

            // Setup test users
            _testUsers = new List<User>
            {
                new User { Id = "user1", Email = "manager1@example.com", FirstName = "Manager", LastName = "One" },
                new User { Id = "user2", Email = "manager2@example.com", FirstName = "Manager", LastName = "Two" }
            };

            // Setup test projects
            _testProjects = new List<Project>
            {
                new Project
                {
                    ProjectID = 1,
                    ProjectName = "Test Project 1",
                    Description = "Test Description 1",
                    StartDate = System.DateTime.Now.AddDays(-10),
                    EndDate = System.DateTime.Now.AddDays(20),
                    ManagerID = "user1",
                    Manager = _testUsers[0]
                },
                new Project
                {
                    ProjectID = 2,
                    ProjectName = "Test Project 2",
                    Description = "Test Description 2",
                    StartDate = System.DateTime.Now.AddDays(-5),
                    EndDate = System.DateTime.Now.AddDays(25),
                    ManagerID = "user2",
                    Manager = _testUsers[1]
                }
            };

            // Setup test tasks
            _testTasks = new List<ProjectTask>
            {
                new ProjectTask
                {
                    TaskID = 1,
                    TaskName = "Task 1",
                    ProjectID = 1,
                    Status = "Not Started"
                },
                new ProjectTask
                {
                    TaskID = 2,
                    TaskName = "Task 2",
                    ProjectID = 2,
                    Status = "In Progress"
                }
            };

            // Setup test project DTOs
            _testProjectDtos = new List<ProjectDto>
            {
                new ProjectDto
                {
                    ProjectID = 1,
                    ProjectName = "Test Project 1",
                    Description = "Test Description 1",
                    StartDate = System.DateTime.Now.AddDays(-10),
                    EndDate = System.DateTime.Now.AddDays(20),
                    ManagerID = "user1",
                    ManagerName = "Manager One",
                    TaskCount = 1
                },
                new ProjectDto
                {
                    ProjectID = 2,
                    ProjectName = "Test Project 2",
                    Description = "Test Description 2",
                    StartDate = System.DateTime.Now.AddDays(-5),
                    EndDate = System.DateTime.Now.AddDays(25),
                    ManagerID = "user2",
                    ManagerName = "Manager Two",
                    TaskCount = 1
                }
            };

            // Setup mock DbContext behavior
            _mockContext.Setup(c => c.Projects).ReturnsDbSet(_testProjects);
            _mockContext.Setup(c => c.Tasks).ReturnsDbSet(_testTasks);

            // Setup mock UserManager
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null, null, null, null, null, null, null, null);

            _mockUserManager.Setup(um => um.Users)
                .Returns(_testUsers.AsQueryable());

            // Setup mock ProjectApiClient
            _mockProjectApiClient = new Mock<ProjectApiClient>();
            _mockProjectApiClient.Setup(api => api.GetProjectsAsync())
                .ReturnsAsync(_testProjectDtos);
            _mockProjectApiClient.Setup(api => api.GetProjectByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _testProjectDtos.FirstOrDefault(p => p.ProjectID == id));

            // Setup TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Create controller
            _controller = new ProjectController(_mockContext.Object, _mockUserManager.Object, _mockProjectApiClient.Object)
            {
                TempData = tempData,
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.Name, "testuser@example.com"),
                            new Claim(ClaimTypes.NameIdentifier, "user1"),
                            new Claim(ClaimTypes.Role, "Admin")
                        }, "mock"))
                    }
                }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfProjectDtos()
        {
            // Arrange is done in constructor

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ProjectDto>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Index_WhenApiReturnsNull_ReturnsViewWithEmptyList()
        {
            // Arrange
            _mockProjectApiClient.Setup(api => api.GetProjectsAsync())
                .ReturnsAsync((List<ProjectDto>)null);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ProjectDto>>(viewResult.Model);
            Assert.Empty(model);
            Assert.Equal("Failed to load projects from API.", _controller.ViewBag.ErrorMessage);
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewResult_WithProjectDto()
        {
            // Arrange
            int validId = 1;

            // Act
            var result = await _controller.Details(validId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProjectDto>(viewResult.Model);
            Assert.Equal(validId, model.ProjectID);
            Assert.Equal("Test Project 1", model.ProjectName);
        }

        [Fact]
        public async Task Details_WithNullId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int invalidId = 999;
            _mockProjectApiClient.Setup(api => api.GetProjectByIdAsync(invalidId))
                .ReturnsAsync((ProjectDto)null);

            // Act
            var result = await _controller.Details(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Get_ReturnsViewResult_WithManagersInViewBag()
        {
            // Arrange is done in constructor

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(_controller.ViewBag.Managers);
        }

        [Fact]
        public async Task Create_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var newProject = new Project
            {
                ProjectName = "New Project",
                Description = "New Description",
                StartDate = System.DateTime.Now,
                EndDate = System.DateTime.Now.AddDays(30),
                ManagerID = "user1"
            };

            _mockContext.Setup(c => c.Add(It.IsAny<Project>()))
                .Callback<Project>(p => _testProjects.Add(p));
            _mockContext.Setup(c => c.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.Create(newProject);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Project created successfully.", _controller.TempData["SuccessMessage"]);

            // Verify Add and SaveChanges were called
            _mockContext.Verify(c => c.Add(It.IsAny<Project>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewResult_WithProject()
        {
            // Arrange
            int validId = 1;
            _mockContext.Setup(c => c.Projects.FindAsync(validId))
                .ReturnsAsync(_testProjects.FirstOrDefault(p => p.ProjectID == validId));

            // Act
            var result = await _controller.Edit(validId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(validId, model.ProjectID);
            Assert.Equal("Test Project 1", model.ProjectName);
            Assert.NotNull(_controller.ViewBag.Managers);
        }

        [Fact]
        public async Task Edit_Get_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Edit(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int invalidId = 999;
            _mockContext.Setup(c => c.Projects.FindAsync(invalidId))
            ;

            // Act
            var result = await _controller.Edit(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var project = _testProjects[0];
            project.ProjectName = "Updated Project";

            _mockContext.Setup(c => c.Update(It.IsAny<Project>()))
                .Callback<Project>(p => {
                    var existingProject = _testProjects.FirstOrDefault(x => x.ProjectID == p.ProjectID);
                    if (existingProject != null)
                    {
                        existingProject.ProjectName = p.ProjectName;
                        existingProject.Description = p.Description;
                    }
                });

            _mockContext.Setup(c => c.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.Edit(project.ProjectID, project);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Project updated successfully.", _controller.TempData["SuccessMessage"]);

            // Verify Update and SaveChanges were called
            _mockContext.Verify(c => c.Update(It.IsAny<Project>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Delete_Get_WithValidId_ReturnsViewResult_WithProject()
        {
            // Arrange
            int validId = 1;
            _mockContext.Setup(c => c.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Project, bool>>>(), default))
                .ReturnsAsync(_testProjects.FirstOrDefault(p => p.ProjectID == validId));

            // Act
            var result = await _controller.Delete(validId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(validId, model.ProjectID);
            Assert.Equal("Test Project 1", model.ProjectName);
        }

        [Fact]
        public async Task Delete_Get_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Delete(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            int invalidId = 999;
            _mockContext.Setup(c => c.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Project, bool>>>(), default))
               ;

            // Act
            var result = await _controller.Delete(invalidId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_NoTasks_RedirectsToIndex()
        {
            // Arrange
            int validId = 1;
            var project = _testProjects.FirstOrDefault(p => p.ProjectID == validId);

            _mockContext.Setup(c => c.Projects.FindAsync(validId))
                .ReturnsAsync(project);

            _mockContext.Setup(c => c.Tasks.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ProjectTask, bool>>>(), default))
                .ReturnsAsync(false);

            _mockContext.Setup(c => c.Projects.Remove(It.IsAny<Project>()))
                .Callback<Project>(p => _testProjects.Remove(p));

            _mockContext.Setup(c => c.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            var result = await _controller.DeleteConfirmed(validId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Project deleted successfully.", _controller.TempData["SuccessMessage"]);

            // Verify Remove and SaveChanges were called
            _mockContext.Verify(c => c.Projects.Remove(It.IsAny<Project>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_WithTasks_ReturnsViewWithError()
        {
            // Arrange
            int validId = 1;
            var project = _testProjects.FirstOrDefault(p => p.ProjectID == validId);

            _mockContext.Setup(c => c.Projects.FindAsync(validId))
                .ReturnsAsync(project);

            _mockContext.Setup(c => c.Tasks.AnyAsync(
                It.IsAny<System.Linq.Expressions.Expression<System.Func<ProjectTask, bool>>>(), default))
                .ReturnsAsync(true);

            _mockContext.Setup(c => c.Projects
                .Include(p => p.Manager)
                .FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<Project, bool>>>(), default))
                .ReturnsAsync(project);

            // Act
            var result = await _controller.DeleteConfirmed(validId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Project>(viewResult.Model);
            Assert.Equal(validId, model.ProjectID);
            Assert.Equal("This project cannot be deleted because it has associated tasks.", _controller.ViewData["ErrorMessage"]);

            // Verify Remove and SaveChanges were NOT called
            _mockContext.Verify(c => c.Projects.Remove(It.IsAny<Project>()), Times.Never);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteConfirmed_WithInvalidId_RedirectsToIndex()
        {
            // Arrange
            int invalidId = 999;
            _mockContext.Setup(c => c.Projects.FindAsync(invalidId))
      ;

            // Act
            var result = await _controller.DeleteConfirmed(invalidId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }
    }
}
