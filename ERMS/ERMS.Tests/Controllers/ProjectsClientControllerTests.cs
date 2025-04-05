using Xunit;
using Moq;
using ERMS.Controllers; // Your MVC Controller namespace
using ERMS.Services;    // Assuming ProjectApiService is here
using ERMS.Models;      // For Project model (or DTO if service returns DTOs)
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net; // For HttpStatusCode
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Castle.Core.Configuration; // For HttpRequestException

namespace ERMS.Tests.Controllers
{
    public class ProjectsClientControllerTests
    {
        // --- Mocks ---
        private readonly Mock<ProjectApiService> _mockProjectService;
        private readonly ProjectsClientController _controller;

        // --- Test Data ---
        // Determine what ProjectApiService returns (Project model or ProjectDto?)
        // Assuming it returns the Project model for now, adjust if it returns DTOs
        private readonly List<Project> _testProjects;

        // --- Constructor (Test Setup) ---
        public ProjectsClientControllerTests()
        {
            // 1. Mock ProjectApiService
            // We mock the service directly. Need to mock its base/dependencies if required.
            // If ProjectApiService has dependencies (like HttpClient), they might need mocks passed here.
            // For simplicity, assume we can mock it directly.
            var mockHttpClient = new Mock<HttpClient>(); // Simple mock, won't make real calls
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            var mockConfiguration = new Mock<IConfiguration>();
            // Note: You *could* use Mock.Of<T>() if you don't need ANY setup on these specific mocks
            // for the tests in *this* file, e.g.:
            // _mockProjectService = new Mock<ProjectApiService>(
            //     Mock.Of<HttpClient>(),
            //     Mock.Of<IHttpContextAccessor>(),
            //     Mock.Of<IConfiguration>()
            // );
            // But creating full mocks allows setup later if needed.

            // Mock HttpContext on the accessor if controller relies on User from it indirectly via service
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);


            // *** Instantiate the Mock<ProjectApiService> passing mock dependencies ***
            _mockProjectService = new Mock<ProjectApiService>(
                mockHttpClient.Object,          // Pass the mock HttpClient's Object
                mockHttpContextAccessor.Object, // Pass the mock IHttpContextAccessor's Object
                mockConfiguration.Object        // Pass the mock IConfiguration's Object
            );
            // Important: If ProjectApiService has *its own* methods that need mocking, they MUST be virtual in ProjectApiService.cs
            // --- End ProjectApiService Mocking Setup ---


            // 2. Prepare Test Data
            _testProjects = new List<Project> { /* ... */ };

            // 3. Create Controller Instance
            _controller = new ProjectsClientController(_mockProjectService.Object); // Inject the mock service // Provide necessary mocks for its constructor if it has one (e.g., Mock.Of<HttpClient>(), Mock.Of<IConfiguration>(), Mock.Of<ILogger<ProjectApiService>>())

            // 2. Prepare Test Data (Models or DTOs matching service return type)
            _testProjects = new List<Project> {
                 new Project { ProjectID = 1, ProjectName = "Client Project One"},
                 new Project { ProjectID = 2, ProjectName = "Client Project Two"}
             };

            // 3. Create Controller Instance
            _controller = new ProjectsClientController(_mockProjectService.Object);

            // Add ControllerContext if needed for things like Url.IsLocalUrl or User claims (not strictly needed for these actions yet)
            // _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        }

        // --- Test Methods ---

        // -- Index (GET) --
        [Fact]
        public async Task Index_ServiceReturnsProjects_ReturnsViewResult_WithProjects()
        {
            // Arrange
            _mockProjectService.Setup(s => s.GetProjectsAsync())
                               .ReturnsAsync(_testProjects); // Mock service success

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Project>>(viewResult.ViewData.Model); // Check model type
            Assert.Equal(2, model.Count());
            Assert.Equal("Client Project One", model.First().ProjectName);
            _mockProjectService.Verify(s => s.GetProjectsAsync(), Times.Once); // Verify service called
        }

        [Fact]
        public async Task Index_ServiceThrowsHttpRequestException_Unauthorized_RedirectsToLogin()
        {
            // Arrange
            var unauthorizedException = new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);
            _mockProjectService.Setup(s => s.GetProjectsAsync())
                               .ThrowsAsync(unauthorizedException); // Mock service throws 401 exception

            // Act
            var result = await _controller.Index();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
            _mockProjectService.Verify(s => s.GetProjectsAsync(), Times.Once);
        }

        [Fact]
        public async Task Index_ServiceThrowsOtherHttpRequestException_ReturnsViewResult_WithEmptyListAndError()
        {
            // Arrange
            var apiErrorException = new HttpRequestException("API Error", null, HttpStatusCode.InternalServerError);
            _mockProjectService.Setup(s => s.GetProjectsAsync())
                              .ThrowsAsync(apiErrorException); // Mock service throws 500 exception

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Project>>(viewResult.ViewData.Model);
            Assert.Empty(model); // Check model is empty list
            Assert.False(_controller.ModelState.IsValid); // Check model state has error
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            _mockProjectService.Verify(s => s.GetProjectsAsync(), Times.Once);
        }

        // -- Details (GET) --
        [Fact]
        public async Task Details_ExistingId_ServiceReturnsProject_ReturnsViewResult_WithProject()
        {
            // Arrange
            int existingId = 1;
            var expectedProject = _testProjects.First(p => p.ProjectID == existingId);
            _mockProjectService.Setup(s => s.GetProjectAsync(existingId))
                              .ReturnsAsync(expectedProject); // Mock service success

            // Act
            var result = await _controller.Details(existingId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<Project>(viewResult.ViewData.Model);
            Assert.Equal(existingId, model.ProjectID);
            Assert.Equal("Client Project One", model.ProjectName);
            _mockProjectService.Verify(s => s.GetProjectAsync(existingId), Times.Once);
        }

        [Fact]
        public async Task Details_ServiceReturnsNull_ReturnsNotFoundResult()
        {
            // Arrange
            int nonExistingId = 999;
            _mockProjectService.Setup(s => s.GetProjectAsync(nonExistingId))
                               .ReturnsAsync((Project?)null); // Mock service returns null (API 404)

            // Act
            var result = await _controller.Details(nonExistingId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            _mockProjectService.Verify(s => s.GetProjectAsync(nonExistingId), Times.Once);
        }

        [Fact]
        public async Task Details_ServiceThrowsHttpRequestException_Unauthorized_RedirectsToLogin()
        {
            // Arrange
            int anyId = 1;
            var unauthorizedException = new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);
            _mockProjectService.Setup(s => s.GetProjectAsync(anyId))
                               .ThrowsAsync(unauthorizedException);

            // Act
            var result = await _controller.Details(anyId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
            _mockProjectService.Verify(s => s.GetProjectAsync(anyId), Times.Once);
        }

        [Fact]
        public async Task Details_ServiceThrowsOtherHttpRequestException_RedirectsToIndexWithError()
        {
            // Arrange
            int anyId = 1;
            var apiErrorException = new HttpRequestException("API Error", null, HttpStatusCode.BadGateway);
            _mockProjectService.Setup(s => s.GetProjectAsync(anyId))
                              .ThrowsAsync(apiErrorException);

            // Act
            var result = await _controller.Details(anyId);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName); // Controller redirects to Index on non-401 error
                                                              // Note: The ModelState error added in the controller isn't easily verifiable after a redirect.
                                                              // Could check TempData if message was stored there instead.
            _mockProjectService.Verify(s => s.GetProjectAsync(anyId), Times.Once);
        }
    }
}