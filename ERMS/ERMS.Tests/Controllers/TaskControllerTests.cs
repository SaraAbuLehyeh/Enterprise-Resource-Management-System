using Xunit;
using Moq;
using ERMS.Controllers;
using ERMS.Data;
using ERMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Moq.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ERMS.Tests.Controllers
{
    public class TaskControllerTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly TaskController _controller;
        private readonly List<ProjectTask> _testTasks;
        private readonly List<Project> _testProjects;
        private readonly List<User> _testUsers;

        public TaskControllerTests()
        {
            // Setup test data
            _testProjects = new List<Project>
            {
                new Project { ProjectID = 1, ProjectName = "Test Project 1" },
                new Project { ProjectID = 2, ProjectName = "Test Project 2" }
            };

            _testUsers = new List<User>
            {
                new User { Id = "user1", Email = "user1@example.com", FirstName = "Test", LastName = "User" },
                new User { Id = "user2", Email = "user2@example.com", FirstName = "Admin", LastName = "User" }
            };

            _testTasks = new List<ProjectTask>
            {
                new ProjectTask
                {
                    TaskID = 1,
                    TaskName = "Task 1",
                    Description = "Test Description 1",
                    ProjectID = 1,
                    Project = _testProjects[0],
                    AssigneeID = "user1",
                    Assignee = _testUsers[0],
                    DueDate = System.DateTime.Now.AddDays(7),
                    Priority = "High",
                    Status = "Not Started"
                },
                new ProjectTask
                {
                    TaskID = 2,
                    TaskName = "Task 2",
                    Description = "Test Description 2",
                    ProjectID = 2,
                    Project = _testProjects[1],
                    AssigneeID = "user2",
                    Assignee = _testUsers[1],
                    DueDate = System.DateTime.Now.AddDays(14),
                    Priority = "Medium",
                    Status = "In Progress"
                }
            };

            // Setup DbContext
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TaskTestDb_{System.Guid.NewGuid()}")
                .Options;
            _mockContext = new Mock<ApplicationDbContext>(options);

            // Setup mock DbSets
            _mockContext.Setup(c => c.Tasks).ReturnsDbSet(_testTasks);
            _mockContext.Setup(c => c.Projects).ReturnsDbSet(_testProjects);
            _mockContext.Setup(c => c.Users).ReturnsDbSet(_testUsers);

            // Mock SaveChangesAsync
            _mockContext.Setup(c => c.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Setup TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Setup controller with User identity
            _controller = new TaskController(_mockContext.Object)
            {
                TempData = tempData,
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                        {
                            new Claim(ClaimTypes.Name, "user1@example.com"),
                            new Claim(ClaimTypes.NameIdentifier, "user1"),
                            new Claim(ClaimTypes.Role, "Admin")
                        }, "mock"))
                    }
                }
            };
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithAllTasks()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ProjectTask>>(viewResult.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewResult_WithTask()
        {
            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProjectTask>(viewResult.Model);
            Assert.Equal(1, model.TaskID);
            Assert.Equal("Task 1", model.TaskName);
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_WithNullId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Details(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Create_Get_ReturnsViewResult_WithSelectLists()
        {
            // Act
            var result = _controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["ProjectID"]);
            Assert.NotNull(viewResult.ViewData["AssigneeID"]);
            Assert.IsType<SelectList>(viewResult.ViewData["ProjectID"]);
            Assert.IsType<SelectList>(viewResult.ViewData["AssigneeID"]);
        }

        [Fact]
        public async Task Create_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var newTask = new ProjectTask
            {
                TaskName = "New Task",
                Description = "New Description",
                ProjectID = 1,
                AssigneeID = "user1",
                DueDate = System.DateTime.Now.AddDays(5),
                Priority = "High",
                Status = "Not Started"
            };

            // Act
            var result = await _controller.Create(newTask);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Verify Add and SaveChanges were called
            _mockContext.Verify(c => c.Add(It.IsAny<ProjectTask>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewResult_WithTask()
        {
            // Act
            var result = await _controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProjectTask>(viewResult.Model);
            Assert.Equal(1, model.TaskID);
            Assert.Equal("Task 1", model.TaskName);
            Assert.NotNull(viewResult.ViewData["ProjectID"]);
            Assert.NotNull(viewResult.ViewData["AssigneeID"]);
        }

        [Fact]
        public async Task Edit_Get_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Edit(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
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
        public async Task Edit_Post_WithValidModel_UpdatesTask()
        {
            // Arrange
            var taskToUpdate = new ProjectTask
            {
                TaskID = 1,
                TaskName = "Updated Task",
                Description = "Updated Description",
                ProjectID = 1,
                AssigneeID = "user1",
                DueDate = System.DateTime.Now.AddDays(10),
                Priority = "Low",
                Status = "Completed"
            };

            // Act
            var result = await _controller.Edit(1, taskToUpdate);

            // Assert
            _mockContext.Verify(c => c.Update(It.IsAny<ProjectTask>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_WithMismatchingId_ReturnsNotFound()
        {
            // Arrange
            var taskToUpdate = new ProjectTask { TaskID = 2 };

            // Act
            var result = await _controller.Edit(1, taskToUpdate);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_WithValidId_ReturnsViewResult_WithTask()
        {
            // Act
            var result = await _controller.Delete(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProjectTask>(viewResult.Model);
            Assert.Equal(1, model.TaskID);
            Assert.Equal("Task 1", model.TaskName);
        }

        [Fact]
        public async Task Delete_Get_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.Delete(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
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
        public async Task DeleteConfirmed_WithValidId_RedirectsToIndex()
        {
            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Verify Remove and SaveChanges were called
            _mockContext.Verify(c => c.Tasks.Remove(It.IsAny<ProjectTask>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task MyTasks_ReturnsViewResult_WithUserTasks()
        {
            // Arrange
            _mockContext.Setup(c => c.Users
                .Where(u => u.Email == "user1@example.com")
                .Select(u => u.Id)
                .FirstOrDefault())
                .Returns("user1");

            // Act
            var result = await _controller.MyTasks();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ProjectTask>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Task 1", model.First().TaskName);
        }

        [Fact]
        public void TaskExists_WithExistingId_ReturnsTrue()
        {
            // Use reflection to access private method
            var method = typeof(TaskController).GetMethod("TaskExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (bool)method.Invoke(_controller, new object[] { 1 });

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void TaskExists_WithNonExistingId_ReturnsFalse()
        {
            // Use reflection to access private method
            var method = typeof(TaskController).GetMethod("TaskExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (bool)method.Invoke(_controller, new object[] { 999 });

            // Assert
            Assert.False(result);
        }
    }
}
