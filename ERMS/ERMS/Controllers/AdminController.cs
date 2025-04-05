// File: Controllers/AdminController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ERMS.Data;
using ERMS.Models;
using ERMS.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // <-- Add Logging
using Microsoft.Data.SqlClient; // <-- Add for SqlParameter

namespace ERMS.Controllers
{
    [Authorize(Roles = "Admin")] // Ensure only Admins can access any action here
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger; // <-- Add ILogger field

        // --- Updated Constructor to inject ILogger ---
        public AdminController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<AdminController> logger) // <-- Inject ILogger
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logger = logger; // <-- Assign logger
        }
        // -------------------------------------------


        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            _logger.LogInformation("Fetching Users list for Admin management.");
            // Add error handling if needed
            var users = await _userManager.Users
                .Include(u => u.Department)
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName) // Good to have ordering
                .Select(u => new UserManagementViewModel
                {
                    UserId = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Department = u.Department != null ? u.Department.DepartmentName : "N/A", // Handle null Department
                    Roles = _userManager.GetRolesAsync(u).Result, // Note: .Result can cause deadlocks in some scenarios, consider refactoring if issues arise
                    IsLocked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now
                })
                .AsNoTracking() // Use AsNoTracking for read-only list
                .ToListAsync();

            return View(users);
        }

        // GET: Admin/EditUserRoles/id
        public async Task<IActionResult> EditUserRoles(string id)
        {
            _logger.LogInformation("Fetching EditUserRoles page for User ID: {UserId}", id);
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("EditUserRoles requested with null or empty ID.");
                return NotFound("User ID cannot be empty.");
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found for EditUserRoles.", id);
                return NotFound($"User with ID {id} not found.");
            }

            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = $"{user.FirstName} {user.LastName}"
            };

            var allRoles = await _roleManager.Roles.ToListAsync(); // Get roles async
            foreach (var role in allRoles)
            {
                if (role.Name != null) // Add null check for safety
                {
                    model.Roles.Add(new RoleSelection
                    {
                        RoleName = role.Name,
                        IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                    });
                }
            }
            model.Roles = model.Roles.OrderBy(r => r.RoleName).ToList(); // Optional: Order roles alphabetically

            return View(model);
        }

        // POST: Admin/EditUserRoles/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRoles(EditUserRolesViewModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.UserId))
            {
                return BadRequest("Invalid submission data.");
            }

            _logger.LogInformation("Attempting to update roles for User ID: {UserId}", model.UserId);
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found during role update POST.", model.UserId);
                return NotFound($"User with ID {model.UserId} not found.");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            // Filter selected roles where RoleName is not null, handle potential nulls from model binding
            var selectedRoles = model.Roles?.Where(x => x.IsSelected && x.RoleName != null).Select(y => y.RoleName!).ToList() ?? new List<string>();

            // Roles to add: Selected roles that the user doesn't currently have
            var rolesToAdd = selectedRoles.Except(userRoles).ToList();
            // Roles to remove: Current roles that are no longer selected
            var rolesToRemove = userRoles.Except(selectedRoles).ToList();

            IdentityResult resultRemove = IdentityResult.Success; // Assume success if nothing to remove
            IdentityResult resultAdd = IdentityResult.Success; // Assume success if nothing to add

            if (rolesToRemove.Any())
            {
                _logger.LogInformation("Removing roles [{Roles}] from user {UserId}", string.Join(", ", rolesToRemove), user.Id);
                resultRemove = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!resultRemove.Succeeded)
                {
                    _logger.LogError("Failed to remove roles from user {UserId}.", user.Id);
                    AddModelErrors(resultRemove); // Add Identity errors to ModelState
                    // Re-populate model for view return on failure
                    model.UserName = $"{user.FirstName} {user.LastName}"; // Keep username display
                                                                          // Roles list might need repopulation, but model state error explains the issue
                    return View(model);
                }
            }

            if (rolesToAdd.Any())
            {
                _logger.LogInformation("Adding roles [{Roles}] to user {UserId}", string.Join(", ", rolesToAdd), user.Id);
                resultAdd = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!resultAdd.Succeeded)
                {
                    _logger.LogError("Failed to add roles to user {UserId}.", user.Id);
                    AddModelErrors(resultAdd); // Add Identity errors to ModelState
                    model.UserName = $"{user.FirstName} {user.LastName}";
                    return View(model);
                }
            }

            _logger.LogInformation("Roles updated successfully for user {UserId}", user.Id);
            TempData["SuccessMessage"] = $"Roles for user {model.Email} updated successfully.";
            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/ToggleUserLock/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLock(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound("User ID required.");

            _logger.LogInformation("Attempting to toggle lock status for User ID: {UserId}", id);
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User ID {UserId} not found for lock toggle.", id);
                return NotFound($"User with ID {id} not found.");
            }

            if (await _userManager.IsLockedOutAsync(user)) // Use IsLockedOutAsync
            {
                // User is locked, so unlock them by setting end date to null or past
                var result = await _userManager.SetLockoutEndDateAsync(user, null); // null unlocks indefinitely
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} unlocked.", id);
                    TempData["SuccessMessage"] = $"User {user.Email} unlocked successfully.";
                }
                else
                {
                    _logger.LogError("Failed to unlock user {UserId}.", id);
                    TempData["ErrorMessage"] = $"Failed to unlock user {user.Email}.";
                    AddModelErrors(result); // Optional: Log errors or add to TempData
                }
            }
            else
            {
                // User is not locked, so lock them indefinitely (or for a defined period)
                var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue); // Lock effectively forever
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserId} locked.", id);
                    TempData["SuccessMessage"] = $"User {user.Email} locked successfully.";
                }
                else
                {
                    _logger.LogError("Failed to lock user {UserId}.", id);
                    TempData["ErrorMessage"] = $"Failed to lock user {user.Email}.";
                    AddModelErrors(result); // Optional
                }
            }

            return RedirectToAction(nameof(Users));
        }

        // *** START: ADDED ACTION FOR STORED PROCEDURE ***
        // POST: /Admin/MarkOverdueTasks
        /// <summary>
        /// Executes the stored procedure to mark overdue tasks.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken] // Protect against CSRF
        public async Task<IActionResult> MarkOverdueTasks()
        {
            _logger.LogInformation("Admin action triggered: MarkOverdueTasks");
            try
            {
                // Define parameters for the stored procedure
                var targetStatus = new SqlParameter("@TargetStatus", "Overdue");
                var currentStatus = new SqlParameter("@CurrentStatus", "Not Started");
                // Get tasks due today or earlier - use Date to compare only dates
                var dueDateThreshold = new SqlParameter("@DueDateThreshold", DateTime.Now.Date);

                // Execute SP using ExecuteSqlRawAsync - returns rows affected by default for UPDATE
                int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.spUpdateTaskStatusBulk @TargetStatus, @CurrentStatus, @DueDateThreshold",
                    targetStatus, currentStatus, dueDateThreshold);

                _logger.LogInformation("Executed spUpdateTaskStatusBulk. Rows affected: {RowsAffected}", rowsAffected);
                TempData["SuccessMessage"] = $"{rowsAffected} 'Not Started' task(s) due on or before today were marked as 'Overdue'.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing spUpdateTaskStatusBulk.");
                TempData["ErrorMessage"] = "An error occurred while trying to mark overdue tasks.";
            }
            // Redirect back to a relevant Admin page, perhaps the Users list or a dedicated Admin Index
            return RedirectToAction(nameof(Index)); // Or RedirectToAction("Users") or create an Admin Index view
        }
        // *** END: ADDED ACTION FOR STORED PROCEDURE ***

        // Example Admin Index Action
        public IActionResult Index()
        {
            // Can display admin actions here
            return View();
        }

        // Helper to add Identity errors to ModelState
        private void AddModelErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}