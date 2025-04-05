using ERMS.Data;
using ERMS.Models;
using ERMS.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users
                .Include(u => u.Department)
                .Select(u => new UserManagementViewModel
                {
                    UserId = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Department = u.Department.DepartmentName,
                    Roles = _userManager.GetRolesAsync(u).Result,
                    IsLocked = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.Now
                })
                .ToListAsync();

            return View(users);
        }

        // GET: Admin/EditUserRoles/id
        public async Task<IActionResult> EditUserRoles(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                UserName = $"{user.FirstName} {user.LastName}"
            };

            foreach (var role in _roleManager.Roles)
            {
                model.Roles.Add(new RoleSelection
                {
                    RoleName = role.Name,
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                });
            }

            return View(model);
        }

        // POST: Admin/EditUserRoles/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRoles(EditUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(x => x.IsSelected).Select(y => y.RoleName);
            var result = await _userManager.RemoveFromRolesAsync(user, userRoles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(user, selectedRoles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }

            return RedirectToAction(nameof(Users));
        }

        // POST: Admin/ToggleUserLock/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserLock(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.Now)
            {
                // User is locked, so unlock them
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                // User is not locked, so lock them
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
            }

            return RedirectToAction(nameof(Users));
        }
    }
}
