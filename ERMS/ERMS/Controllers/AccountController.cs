// File: Controllers/AccountController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ERMS.Data;
using ERMS.Models;
using ERMS.ViewModels;
using System.Threading.Tasks;
using System.Linq;
// using ERMS.Services; // Remove if AuthApiService is no longer used
using Microsoft.Extensions.Logging; // Required for logging
using Microsoft.EntityFrameworkCore; // Required for SelectList/Departments access

namespace ERMS.Controllers
{
    /// <summary>
    /// Controller for handling user authentication (Cookie-based) and account management.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger; // Added logger

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger) // Inject logger
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger; // Assign logger
        }

        /// <summary>
        /// Displays the login page.
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Ensure login page can be accessed by anyone
        public IActionResult Login(string? returnUrl = null) // Use nullable reference type
        {
            // Clear any existing external cookie to ensure a clean login process
            // await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme); // Consider if using external logins

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }


        /// <summary>
        /// Processes the standard login form submission using Identity Cookies.
        /// </summary>
        [HttpPost]
        [AllowAnonymous] // Ensure login post can be accessed by anyone
        [ValidateAntiForgeryToken] // Protect against CSRF
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null) // Use nullable reference type
        {
            returnUrl ??= Url.Content("~/"); // Default to home page if returnUrl is null or empty

            if (ModelState.IsValid)
            {
                // Attempt to sign in using password and cookie scheme.
                // Checks for lockout, 2FA etc.
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} logged in successfully using Cookie Authentication.", model.Email);
                    // Clear any JWT potentially left in sessionStorage from a previous session
                    // Note: This server-side code cannot clear browser sessionStorage directly.
                    // Client-side JavaScript on logout is responsible for clearing sessionStorage.

                    return LocalRedirect(returnUrl); // Redirect to intended page or home
                }
                // Handle other login results (2FA, Lockout, Not Allowed)
                if (result.RequiresTwoFactor)
                {
                    // Redirect to 2FA page if configured
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User {Email} attempted to log in but account is locked out.", model.Email);
                    return RedirectToAction(nameof(Lockout)); // Use nameof for safety
                }
                else
                {
                    // Includes incorrect password or user not found (Identity combines these)
                    _logger.LogWarning("Invalid login attempt for user {Email}.", model.Email);
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model); // Return to login view with error
                }
            }

            // If ModelState is invalid, redisplay form
            return View(model);
        }

        /// <summary>
        /// Displays the registration page.
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Allow access to registration page
        public IActionResult Register()
        {
            // Populate departments dropdown for the registration form
            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "DepartmentID", "DepartmentName");
            // Return the View, don't redirect here
            return View();
        }

        /// <summary>
        /// Processes the registration form submission.
        /// </summary>
        [HttpPost]
        [AllowAnonymous] // Allow access to registration post
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if department exists (important as it's required in User model)
                var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentID == model.DepartmentID);
                if (!departmentExists)
                {
                    ModelState.AddModelError(nameof(model.DepartmentID), "Selected department does not exist.");
                    // Repopulate dropdown and return view if validation fails early
                    ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "DepartmentID", "DepartmentName", model.DepartmentID);
                    return View(model);
                }

                var user = new User
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    HireDate = model.HireDate, // Assuming HireDate is required and non-null in ViewModel too
                    DepartmentID = model.DepartmentID // Assuming DepartmentID is required and non-null in ViewModel too
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User {Email} created a new account with password.", model.Email);
                    // Assign default role - Ensure "Employee" role exists via seeding!
                    var roleResult = await _userManager.AddToRoleAsync(user, "Employee");
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogError("Failed to add default 'Employee' role to user {Email}", model.Email);
                        // Decide how to handle this - maybe add error, maybe proceed?
                        // ModelState.AddModelError(string.Empty, "User created but failed to assign default role.");
                    }

                    // Optionally sign the user in immediately after registration
                    await _signInManager.SignInAsync(user, isPersistent: false); // Creates the login cookie
                    _logger.LogInformation("User {Email} automatically signed in after registration.", model.Email);

                    return RedirectToAction("Index", "Home"); // Redirect to Home/Dashboard
                }

                // If user creation failed, add errors to ModelState
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed (ModelState invalid or user creation failed)
            // Repopulate departments dropdown
            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "DepartmentID", "DepartmentName", model.DepartmentID); // Pass selected value back
            return View(model); // Return the view with the model to show validation errors
        }

        // *** REMOVED ApiLogin method - No longer needed with Solution C ***
        /*
        [HttpPost]
        public async Task<IActionResult> ApiLogin(LoginViewModel model)
        { ... }
        */

        /// <summary>
        /// Logs the user out (clears the cookie).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        // No [Authorize] needed, user might already be logged out but wants to ensure it
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name; // Get username before signing out for logging
            await _signInManager.SignOutAsync(); // Clears the Identity.Application cookie
            _logger.LogInformation("User {UserName} logged out.", userName ?? "<Unknown>");

            // IMPORTANT: Client-side JavaScript MUST clear the JWT from sessionStorage on logout.
            // This server-side action cannot clear sessionStorage in the browser.

            return RedirectToAction("Index", "Home"); // Redirect to home page after logout
        }

        /// <summary>
        /// Displays the lockout page.
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }

        /// <summary>
        /// Displays the access denied page.
        /// </summary>
        [HttpGet]
        // User must be logged in (via cookie) to be denied access, so no AllowAnonymous
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Keep RedirectToLocal helper method
        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                // Prevent open redirect vulnerability, always redirect locally
                return RedirectToAction("Index", "Dashboard"); // Or "Home", "Index"
            }
        }
    }
}