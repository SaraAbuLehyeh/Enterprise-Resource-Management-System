// File: Controllers/ProjectController.cs

using ERMS.Data; // Keep for DbContext if needed elsewhere
using ERMS.DTOs;       // <-- Add using
using ERMS.HttpClients; // <-- Add using
using ERMS.Models;
// using Microsoft.EntityFrameworkCore; // Keep if needed for PopulateManagersDropDownList
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; // Keep for UserManager
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // Keep for ToListAsync in helper


namespace ERMS.Controllers
{
    [Authorize] // Uses Cookie Auth for MVC Controller access
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context; // Keep for dropdown population
        private readonly UserManager<User> _userManager;
        private readonly ProjectApiClient _projectApiClient; // <-- Inject the API client

        public ProjectController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ProjectApiClient projectApiClient) // <-- Accept client in constructor
        {
            _context = context;
            _userManager = userManager;
            _projectApiClient = projectApiClient; // <-- Assign the client
        }

        // GET: Project
        public async Task<IActionResult> Index()
        {
            // --- Call API Client to get projects ---
            // This works now because GET /api/ProjectsApi has [AllowAnonymous] temporarily
            List<ProjectDto>? projects = await _projectApiClient.GetProjectsAsync();

            if (projects == null)
            {
                ViewBag.ErrorMessage = "Failed to load projects from API.";
                projects = new List<ProjectDto>();
            }

            // Pass List<ProjectDto> to the View
            // View (Index.cshtml) MUST be updated to use ProjectDto
            return View(projects);
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return BadRequest("Project ID is required.");
            }

            // --- Call API Client to get project details ---
            // This works now because GET /api/ProjectsApi/{id} has [AllowAnonymous] temporarily
            ProjectDto? project = await _projectApiClient.GetProjectByIdAsync(id.Value);

            if (project == null)
            {
                return NotFound(); // API Client returned null (likely 404 from API)
            }

            // Pass the ProjectDto to the View
            // View (Details.cshtml) MUST be updated to use ProjectDto
            return View(project);
        }

        // --- CREATE, EDIT, DELETE actions remain unchanged (using DbContext directly) ---
        // --- You could modify one of these too for demo, but GET is simplest ---

        // GET: Project/Create
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            await PopulateManagersDropDownList();
            // Create view still likely binds to Project model or a specific CreateViewModel
            // If it binds to Project, that's fine for this setup.
            return View();
        }

        // POST: Project/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([Bind("ProjectName,Description,StartDate,EndDate,ManagerID")] Project project) // Still binding to Model
        {

            // --- SAVE DATA DIRECTLY TO DB ---
            _context.Add(project);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Project created successfully.";
            return RedirectToAction(nameof(Index));

            await PopulateManagersDropDownList(project.ManagerID);
            return View(project);
        }

        // GET: Project/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var project = await _context.Projects.FindAsync(id); // Get model for editing form
            if (project == null) return NotFound();
            await PopulateManagersDropDownList(project.ManagerID);
            return View(project); // Pass model to view
        }

        // POST: Project/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("ProjectID,ProjectName,Description,StartDate,EndDate,ManagerID")] Project project)
        {
            if (id != project.ProjectID) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project); // Update model directly
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Project updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await ProjectExistsAsync(project.ProjectID)) return NotFound(); else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateManagersDropDownList(project.ManagerID);
            return View(project); // Return model to view on failure
        }

        // GET: Project/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var project = await _context.Projects.Include(p => p.Manager).FirstOrDefaultAsync(m => m.ProjectID == id); // Get model for confirmation
            if (project == null) return NotFound();
            var hasTasks = await _context.Tasks.AnyAsync(t => t.ProjectID == id);
            if (hasTasks) ViewData["ErrorMessage"] = "This project cannot be deleted because it has associated tasks.";
            return View(project); // Pass model to view
        }

        // POST: Project/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return RedirectToAction(nameof(Index));

            var hasTasks = await _context.Tasks.AnyAsync(t => t.ProjectID == id);
            if (hasTasks)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete project with assigned tasks. Please remove or reassign tasks first.");
                project = await _context.Projects.Include(p => p.Manager).FirstOrDefaultAsync(m => m.ProjectID == id); // Reload for view
                ViewData["ErrorMessage"] = "This project cannot be deleted because it has associated tasks.";
                return View(project); // Return view with error
            }
            try
            {
                _context.Projects.Remove(project); // Delete directly
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Project deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to delete project.");
                project = await _context.Projects.Include(p => p.Manager).FirstOrDefaultAsync(m => m.ProjectID == id); // Reload for view
                return View(project);
            }
        }

        // Helper to populate dropdown - uses DbContext/UserManager
        private async Task PopulateManagersDropDownList(object? selectedManager = null)
        {
            var managersQuery = _userManager.Users
                                        .OrderBy(u => u.LastName)
                                        .ThenBy(u => u.FirstName);
            ViewBag.Managers = new SelectList(await managersQuery.ToListAsync(), "Id", "Email", selectedManager);
        }

        // Helper to check existence - uses DbContext
        private async Task<bool> ProjectExistsAsync(int id)
        {
            return await _context.Projects.AnyAsync(e => e.ProjectID == id);
        }
    }
}