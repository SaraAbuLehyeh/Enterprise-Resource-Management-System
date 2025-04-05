using Xunit;
using Moq;
using ERMS.Controllers; // Controller namespace
using ERMS.Models;      // For ErrorViewModel
using Microsoft.AspNetCore.Mvc; // For ViewResult, IActionResult etc.
using Microsoft.Extensions.Logging; // For ILogger
using System.Diagnostics; // For Activity
using Microsoft.AspNetCore.Http; // For HttpContext

namespace ERMS.Tests.Controllers
{
    public class HomeControllerTests
    {
        // --- Mocks ---
        private readonly Mock<ILogger<HomeController>> _mockLogger;
        private readonly HomeController _controller;

        // --- Constructor (Test Setup) ---
        public HomeControllerTests()
        {
            // 1. Mock Logger
            _mockLogger = new Mock<ILogger<HomeController>>();

            // 2. Create Controller Instance
            _controller = new HomeController(_mockLogger.Object);

            // 3. Mock HttpContext for Error action (optional but good practice)
            // We need HttpContext to resolve TraceIdentifier if Activity.Current is null
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext() // Provide a basic HttpContext
            };
        }

        // --- Test Methods ---

        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange (Done in constructor)

            // Act
            var result = _controller.Index();

            // Assert
            // Check that the result is a ViewResult
            var viewResult = Assert.IsType<ViewResult>(result);
            // Optional: Check that no specific model is passed (or the expected model type if any)
            Assert.Null(viewResult.ViewData.Model);
            // Optional: Check that the view name is null or "Index" (meaning default view)
            Assert.Null(viewResult.ViewName); // Or Assert.Equal("Index", viewResult.ViewName);
        }

        [Fact]
        public void Privacy_ReturnsViewResult()
        {
            // Arrange (Done in constructor)

            // Act
            var result = _controller.Privacy();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewData.Model);
            Assert.Null(viewResult.ViewName); // Or Assert.Equal("Privacy", viewResult.ViewName);
        }

        [Fact]
        public void Error_ReturnsViewResult_WithErrorViewModel()
        {
            // Arrange (Done in constructor)
            // Optional: Setup Activity.Current if you need to test that specific path
            // var activity = new Activity("TestActivity").Start();

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ErrorViewModel>(viewResult.ViewData.Model);

            // Check that RequestId is populated (either from Activity or TraceIdentifier)
            Assert.NotNull(model.RequestId);
            Assert.False(string.IsNullOrEmpty(model.RequestId));

            // Optional: Cleanup Activity if started
            // activity?.Stop();
        }

        [Fact]
        public void Error_ActivityIsNull_UsesTraceIdentifier()
        {
            // Arrange
            // Ensure Activity.Current is null (usually is in test context unless explicitly set)
            Activity.Current = null;
            // Set a TraceIdentifier on the mock HttpContext
            var expectedTraceId = "TestTraceId123";
            _controller.HttpContext.TraceIdentifier = expectedTraceId;


            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ErrorViewModel>(viewResult.ViewData.Model);
            Assert.Equal(expectedTraceId, model.RequestId);
        }

        [Fact]
        public void Error_ActivityIsNotNull_UsesActivityId()
        {
            // Arrange
            // Start an activity to ensure Activity.Current is not null
            var activity = new Activity("TestActivityForError").Start();
            var expectedActivityId = activity.Id; // Get the ID of the started activity
            Assert.NotNull(expectedActivityId); // Ensure activity ID is not null

            // Act
            var result = _controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<ErrorViewModel>(viewResult.ViewData.Model);
            Assert.Equal(expectedActivityId, model.RequestId); // Check it used the activity ID

            // Cleanup
            activity.Stop();
        }
    }
}