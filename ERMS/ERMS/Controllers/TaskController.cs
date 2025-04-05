using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ERMS.Data;
using ERMS.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // <-- Add Logging namespace
using System.Security.Claims; // <-- Add for User ID lookup



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
      

        // GET: Task
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Fetching Task Index page.");
            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .ToListAsync();
            return View(tasks);
        }

        // GET: Task/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            _logger.LogInformation("Fetching Details for Task ID: {TaskId}", id);
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(m => m.TaskID == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // GET: Task/Create
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            _logger.LogInformation("Displaying Create Task page.");
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectName");
            ViewData["AssigneeID"] = new SelectList(_context.Users, "Id", "Email");
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
            try
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception
                ModelState.AddModelError("", "Unable to save changes. " + ex.Message);
            }

           
            
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectName", task.ProjectID);
            ViewData["AssigneeID"] = new SelectList(_context.Users, "Id", "Email", task.AssigneeID);
            return View(task);
        }

        // GET: Task/Edit/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectName", task.ProjectID);
            ViewData["AssigneeID"] = new SelectList(_context.Users, "Id", "Email", task.AssigneeID);
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
                return NotFound();
            }

  
                try
                {
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.TaskID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProjectID"] = new SelectList(_context.Projects, "ProjectID", "ProjectName", task.ProjectID);
            ViewData["AssigneeID"] = new SelectList(_context.Users, "Id", "Email", task.AssigneeID);
            return View(task);
        }

        // GET: Task/Delete/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks
                .Include(t => t.Project)
                .Include(t => t.Assignee)
                .FirstOrDefaultAsync(m => m.TaskID == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        // POST: Task/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Task/MyTasks
        [Authorize]
        public async Task<IActionResult> MyTasks()
        {
            var userId = _context.Users
                .Where(u => u.Email == User.Identity.Name)
                .Select(u => u.Id)
                .FirstOrDefault();

            var tasks = await _context.Tasks
                .Include(t => t.Project)
                .Where(t => t.AssigneeID == userId)
                .ToListAsync();

            return View(tasks);
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.TaskID == id);
        }
    }
}
