using Xunit;
using Moq;
using ERMS.Controllers;
using ERMS.Data;
using ERMS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq.EntityFrameworkCore;

namespace ERMS.Tests.Controllers
{
    public class DepartmentControllerTests
    {
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly DepartmentController _controller;
        private readonly List<Department> _testDepartments;

        public DepartmentControllerTests()
        {
            // Setup test data
            _testDepartments = new List<Department>
            {
                new Department { DepartmentID = 1, DepartmentName = "Human Resources" },
                new Department { DepartmentID = 2, DepartmentName = "Information Technology" },
                new Department { DepartmentID = 3, DepartmentName = "Finance" }
            };

            // Setup DbContext options
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"DepartmentTestDb_{System.Guid.NewGuid()}")
                .Options;
            _mockContext = new Mock<ApplicationDbContext>(options);

            // Setup mock DbSet
            _mockContext.Setup(c => c.Departments).ReturnsDbSet(_testDepartments);

            // Setup SaveChangesAsync
            _mockContext.Setup(c => c.SaveChangesAsync(default))
                .ReturnsAsync(1); // 1 entity affected

            // Setup TempData
            var tempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Create controller
            _controller = new DepartmentController(_mockContext.Object)
            {
                TempData = tempData
            };
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfDepartments()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Department>>(viewResult.Model);
            Assert.Equal(3, model.Count());
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewResult_WithDepartment()
        {
            // Act
            var result = await _controller.Details(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Department>(viewResult.Model);
            Assert.Equal(1, model.DepartmentID);
            Assert.Equal("Human Resources", model.DepartmentName);
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
        public void Create_Get_ReturnsViewResult()
        {
            // Act
            var result = _controller.Create();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var newDepartment = new Department
            {
                DepartmentName = "Marketing"
            };

            // Act
            var result = await _controller.Create(newDepartment);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Verify Add and SaveChanges were called
            _mockContext.Verify(c => c.Add(It.IsAny<Department>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var invalidDepartment = new Department(); // Empty model
            _controller.ModelState.AddModelError("DepartmentName", "Required");

            // Act
            var result = await _controller.Create(invalidDepartment);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Department>(viewResult.Model);
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewResult_WithDepartment()
        {
            // Act
            var result = await _controller.Edit(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Department>(viewResult.Model);
            Assert.Equal(1, model.DepartmentID);
            Assert.Equal("Human Resources", model.DepartmentName);
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
        public async Task Edit_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            var department = new Department
            {
                DepartmentID = 1,
                DepartmentName = "HR Updated"
            };

            // Act
            var result = await _controller.Edit(1, department);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);

            // Verify Update and SaveChanges were called
            _mockContext.Verify(c => c.Update(It.IsAny<Department>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Edit_Post_WithMismatchingId_ReturnsNotFound()
        {
            // Arrange
            var department = new Department { DepartmentID = 2 };

            // Act
            var result = await _controller.Edit(1, department);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_WithInvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var department = new Department { DepartmentID = 1 };
            _controller.ModelState.AddModelError("DepartmentName", "Required");

            // Act
            var result = await _controller.Edit(1, department);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<Department>(viewResult.Model);
        }

        [Fact]
        public async Task Delete_Get_WithValidId_ReturnsViewResult_WithDepartment()
        {
            // Act
            var result = await _controller.Delete(1);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Department>(viewResult.Model);
            Assert.Equal(1, model.DepartmentID);
            Assert.Equal("Human Resources", model.DepartmentName);
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
            _mockContext.Verify(c => c.Departments.Remove(It.IsAny<Department>()), Times.Once);
            _mockContext.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public void DepartmentExists_WithExistingId_ReturnsTrue()
        {
            // Use reflection to access private method
            var method = typeof(DepartmentController).GetMethod("DepartmentExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (bool)method.Invoke(_controller, new object[] { 1 });

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void DepartmentExists_WithNonExistingId_ReturnsFalse()
        {
            // Use reflection to access private method
            var method = typeof(DepartmentController).GetMethod("DepartmentExists",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var result = (bool)method.Invoke(_controller, new object[] { 999 });

            // Assert
            Assert.False(result);
        }
    }
}
