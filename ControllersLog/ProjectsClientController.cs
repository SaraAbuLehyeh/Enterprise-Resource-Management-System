using ERMS.Models;
using ERMS.Services;
using Microsoft.AspNetCore.Mvc;

namespace ERMS.Controllers
{
    public class ProjectsClientController : Controller
    {
        private readonly ProjectApiService _projectService;

        public ProjectsClientController(ProjectApiService projectService)
        {
            _projectService = projectService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var projects = await _projectService.GetProjectsAsync();
                return View(projects);
            }
            catch (HttpRequestException ex)
            {
                // Handle unauthorized or other API errors
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Account");
                }

                ModelState.AddModelError("", "Error retrieving projects: " + ex.Message);
                return View(new List<Project>());
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var project = await _projectService.GetProjectAsync(id);
                if (project == null)
                {
                    return NotFound();
                }
                return View(project);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction("Login", "Account");
                }

                ModelState.AddModelError("", "Error retrieving project details: " + ex.Message);
                return RedirectToAction(nameof(Index));
            }
        }

        // Add Create, Edit, Delete actions as needed
    }
}
