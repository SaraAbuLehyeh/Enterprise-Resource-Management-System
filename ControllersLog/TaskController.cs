using ERMS.Data;
using ERMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging; // <-- Add Logging namespace
using System;
using System.Linq;
using System.Security.Claims; // <-- Add for User ID lookup
using System.Threading.Tasks;


namespace ERMS.Controllers
{
    [Authorize]
    public class TaskController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskController> _logger; // <-- Declare Logger field

        // --- Inject ILogger in constructor ---
        public TaskController(ApplicationDbContext context, ILogger<TaskController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Assign logger
        }
        // ---------------------------------

        // GET: Task
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching Task Index page.");
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                 .OrderBy(t => t.DueDate) // Optional ordering
                .ToListAsync();
            return View(tasks);
        }

        // GET: Task/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Fetching Details for Task ID: {TaskId}", id);
            if (id == null)
            {
                _logger.LogWarning("Details requested with null ID.");
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                 .AsNoTracking() // Use for read-only
                .FirstOrDefaultAsync(m => m.TaskID == id);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {TaskId} not found for Details.", id);
                return NotFound($"Task with ID {id} not found.");
            }

            return View(task);
        }

        // GET: Task/Create
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create() // Make async
        {
            _logger.LogInformation("Displaying Create Task page.");
            await PopulateDropdowns(); // Call async helper
            return View();
        }

        // POST: Task/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([Bind("TaskName,Description,ProjectID,AssigneeID,DueDate,Priority,Status")] ProjectTask task)
        {
            _logger.LogInformation("Attempting to Create Task: {TaskName} for Project: {ProjectId}, Assignee: {AssigneeId}",
                task?.TaskName, task?.ProjectID, task?.AssigneeID);

            // Ensure ProjectID and AssigneeID were provided
            if (task.ProjectID <= 0) ModelState.AddModelError(nameof(task.ProjectID), "Project must be selected.");
            if (string.IsNullOrEmpty(task.AssigneeID)) ModelState.AddModelError(nameof(task.AssigneeID), "Assignee must be selected.");


         
                try
                {
                    // Attach Existing Entities
                    var projectStub = new Project { ProjectID = task.ProjectID };
                    _context.Attach(projectStub);
                    var userStub = new User { Id = task.AssigneeID };
                    _context.Attach(userStub);

                    _context.Add(task);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully created Task ID: {TaskId}", task.TaskID);
                    TempData["SuccessMessage"] = $"Task '{task.TaskName}' created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "DB Error Creating Task: {TaskName}. Inner: {InnerEx}", task?.TaskName, ex.InnerException?.Message);
                    ModelState.AddModelError("", "Unable to save changes. " +
                        "Ensure the selected Project and Assignee exist. " +
                         ex.InnerException?.Message); // Provide more specific error if possible
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error Creating Task {TaskName}.", task?.TaskName);
                    ModelState.AddModelError("", "An unexpected error occurred saving the task.");
                }
            
         

            // If ModelState invalid OR exception occurred, repopulate and return view
            await PopulateDropdowns(task.ProjectID, task.AssigneeID); // Repopulate dropdowns
            return View(task);
        }

        // GET: Task/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            _logger.LogInformation("Fetching Edit page for Task ID: {TaskId}", id);
            if (id == null) { _logger.LogWarning("Edit requested with null ID."); return NotFound(); }

            var task = await _context.Tasks.FindAsync(id); // FindAsync is fine here

            if (task == null) { _logger.LogWarning("Task with ID {TaskId} not found for Edit.", id); return NotFound($"Task with ID {id} not found."); }

            await PopulateDropdowns(task.ProjectID, task.AssigneeID); // Populate dropdowns
            return View(task);
        }

        // POST: Task/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("TaskID,TaskName,Description,ProjectID,AssigneeID,DueDate,Priority,Status")] ProjectTask task)
        {
            if (id != task.TaskID)
            {
                _logger.LogWarning("Edit POST ID mismatch. Route ID: {RouteId}, Model ID: {ModelId}", id, task.TaskID);
                return NotFound("ID mismatch during edit.");
            }

            _logger.LogInformation("Attempting to Update Task ID: {TaskId}", id);

            // Ensure ProjectID and AssigneeID are still valid after edit attempt (if they can be changed)
            if (task.ProjectID <= 0) ModelState.AddModelError(nameof(task.ProjectID), "Project must be selected.");
            if (string.IsNullOrEmpty(task.AssigneeID)) ModelState.AddModelError(nameof(task.AssigneeID), "Assignee must be selected.");

            if (ModelState.IsValid)
            {
                try
                {
                    // --- Re-check related entities IF they could have changed and if FK constraints might fail ---
                    // var projectExists = await _context.Projects.AnyAsync(p => p.ProjectID == task.ProjectID);
                    // var assigneeExists = await _context.Users.AnyAsync(u => u.Id == task.AssigneeID);
                    // if (!projectExists || !assigneeExists) { /* Add ModelStateError, fall through */ }
                    // --- OR rely on DB constraints and catch DbUpdateException ---

                    _context.Update(task);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully updated Task ID: {TaskId}", id);
                    TempData["SuccessMessage"] = $"Task '{task.TaskName}' updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Concurrency error updating Task ID: {TaskId}", id);
                    var taskExists = await TaskExistsAsync(task.TaskID);
                    if (!taskExists)
                    {
                        return NotFound($"Task with ID {id} was deleted by another user.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "The record was modified by another user. Your edit was canceled.");
                    }
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Database error updating Task ID: {TaskId}. Inner: {InnerEx}", id, ex.InnerException?.Message);
                    ModelState.AddModelError("", "Unable to save changes. Ensure Project/Assignee exist. " + ex.InnerException?.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error updating Task ID: {TaskId}.", id);
                    ModelState.AddModelError("", "An unexpected error occurred.");
                }
            }
            else
            {
                _logger.LogWarning("Update Task failed due to invalid ModelState for ID: {TaskId}", id);
            }


            // If ModelState invalid or exception occurred
            _logger.LogDebug("Returning Edit view with invalid model state for Task ID: {TaskId}", id);
            await PopulateDropdowns(task.ProjectID, task.AssigneeID); // Repopulate dropdowns
            return View(task);
        }

        // GET: Task/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            _logger.LogInformation("Fetching Delete confirmation page for Task ID: {TaskId}", id);
            if (id == null) { _logger.LogWarning("Delete GET requested with null ID."); return NotFound(); }

            // Include related data for display on confirmation page
            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .AsNoTracking() // Read-only
                .FirstOrDefaultAsync(m => m.TaskID == id);

            if (task == null) { _logger.LogWarning("Task with ID {TaskId} not found for Delete GET.", id); return NotFound($"Task with ID {id} not found."); }

            return View(task);
        }

        // POST: Task/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            _logger.LogInformation("Attempting to Delete Task ID: {TaskId}", id);
            // Find task again to ensure it exists before attempting delete
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
            {
                _logger.LogWarning("Attempted to delete Task ID: {TaskId}, but it was not found.", id);
                TempData["WarningMessage"] = $"Task with ID {id} no longer exists.";
                return RedirectToAction(nameof(Index)); // Redirect if already gone
            }

            try
            {
                _context.Remove(task); // Use DbContext.Remove
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted Task ID: {TaskId}", id);
                TempData["SuccessMessage"] = $"Task '{task.TaskName}' deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Log error, usually constraint violation if task is referenced elsewhere unexpectedly
                _logger.LogError(ex, "Database error deleting Task ID: {TaskId}. Inner: {InnerEx}", id, ex.InnerException?.Message);
                ModelState.AddModelError("", "Unable to delete task. Try again, and if the problem persists see your system administrator.");
                // Return the confirmation view again with the error
                // Need to re-fetch the task with includes for the view model
                task = await _context.Tasks.Include(t => t.Project).Include(t => t.Assignee).AsNoTracking().FirstOrDefaultAsync(m => m.TaskID == id);
                return View(task); // Show delete view again with error
            }
            catch (Exception ex) // Catch unexpected errors
            {
                _logger.LogError(ex, "Unexpected error deleting Task ID: {TaskId}.", id);
                ModelState.AddModelError("", "An unexpected error occurred.");
                task = await _context.Tasks.Include(t => t.Project).Include(t => t.Assignee).AsNoTracking().FirstOrDefaultAsync(m => m.TaskID == id);
                return View(task);
            }
        }

        // GET: Task/MyTasks
        [Authorize] // Any authenticated user can see their own tasks
        public async Task<IActionResult> MyTasks()
        {
            _logger.LogInformation("Fetching MyTasks for user: {UserName}", User.Identity?.Name);
            // Get current user's ID using ClaimsPrincipal helper (more robust)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Could not find UserId claim for user {UserName} in MyTasks.", User.Identity?.Name);
                return Unauthorized("User ID could not be determined."); // Or redirect to login?
            }

            // Query tasks assigned to the current user
            var tasks = await _context.Tasks
                .Include(t => t.Project) // Include project info
                .Where(t => t.AssigneeID == userId)
                 .OrderBy(t => t.DueDate)
                 .AsNoTracking() // Read-only
                .ToListAsync();

            _logger.LogInformation("Found {TaskCount} tasks for user ID: {UserId}", tasks.Count, userId);
            return View(tasks); // Pass list to the MyTasks view
        }

        // Make helper async
        private async Task<bool> TaskExistsAsync(int id)
        {
            return await _context.Tasks.AnyAsync(e => e.TaskID == id);
        }

        // --- ADD Helper method to populate dropdowns ---
        private async Task PopulateDropdowns(object? selectedProject = null, object? selectedAssignee = null)
        {
            try
            {
                // Fetch projects ordered by name
                ViewData["ProjectID"] = new SelectList(await _context.Projects.OrderBy(p => p.ProjectName).AsNoTracking().ToListAsync(), "ProjectID", "ProjectName", selectedProject);

                // Fetch users ordered by email or name
                ViewData["AssigneeID"] = new SelectList(await _context.Users.OrderBy(u => u.Email).AsNoTracking().ToListAsync(), "Id", "Email", selectedAssignee); // Use Email as display text
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdowns for Task Create/Edit.");
                // Add error to ViewData? Or handle differently?
                ViewData["DropdownError"] = "Error loading projects or assignees.";
                ViewData["ProjectID"] = new SelectList(Enumerable.Empty<SelectListItem>(), "ProjectID", "ProjectName");
                ViewData["AssigneeID"] = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "Email");
            }
        }
        // -------------------------------------------
    }
}