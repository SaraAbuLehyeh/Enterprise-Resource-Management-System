using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ERMS.Data;
using ERMS.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ERMS.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get current user ID
            var userId = _context.Users
                .Where(u => u.Email == User.Identity.Name)
                .Select(u => u.Id)
                .FirstOrDefault();

            // Get user's tasks
            var userTasks = await _context.Tasks
                .Include(t => t.Project)
                .Where(t => t.AssigneeID == userId)
                .ToListAsync();

            // Get counts for dashboard
            ViewBag.TotalTasks = userTasks.Count;
            ViewBag.CompletedTasks = userTasks.Count(t => t.Status == "Completed");
            ViewBag.InProgressTasks = userTasks.Count(t => t.Status == "In Progress");
            ViewBag.PendingTasks = userTasks.Count(t => t.Status == "Not Started");

            // Get upcoming deadlines (tasks due in the next 7 days)
            ViewBag.UpcomingDeadlines = userTasks
                .Where(t => t.DueDate.Date >= DateTime.Now.Date &&
                           t.DueDate.Date <= DateTime.Now.AddDays(7).Date &&
                           t.Status != "Completed")
                .OrderBy(t => t.DueDate)
                .ToList();

            // For managers and admins, get project statistics
            if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            {
                var projects = await _context.Projects
                    .Include(p => p.Tasks)
                    .ToListAsync();

                ViewBag.TotalProjects = projects.Count;
                ViewBag.ProjectsWithTasks = projects.Count(p => p.Tasks.Any());
                ViewBag.Projects = projects;
            }

            return View();
        }
    }
}
