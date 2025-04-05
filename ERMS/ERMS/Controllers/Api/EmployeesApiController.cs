using ERMS.Data;
using ERMS.DTOs;
using ERMS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Required
using Microsoft.AspNetCore.Authorization;          // Required
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ERMS.Controllers.Api
{
    /// <summary>
    /// API Controller for managing Employee resources (Users). (Secured)
    /// Requires JWT authentication. Specific actions require Admin role.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Require JWT for all actions
    public class EmployeesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<EmployeesApiController> _logger;

        public EmployeesApiController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<EmployeesApiController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/EmployeesApi
        /// <summary>
        /// Retrieves a list of all employees (Users). Requires authentication.
        /// Consider restricting further based on roles if needed.
        /// </summary>
        [HttpGet]
        // Inherits controller auth - could add [Authorize(Roles="Admin,Manager")] if needed
        [ProducesResponseType(typeof(IEnumerable<EmployeeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<EmployeeDto>>> GetEmployees()
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation("Attempting to retrieve all employees (users).");
                var users = await _context.Users.Include(u => u.Department)
                                        .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToListAsync();
                var employeeDtos = users.Select(u => new EmployeeDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    HireDate = u.HireDate,
                    DepartmentID = u.DepartmentID,
                    DepartmentName = u.Department?.DepartmentName
                }).ToList();
                _logger.LogInformation($"Successfully retrieved {employeeDtos.Count} employees.");
                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving employees.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred retrieving employees.");
            }
        }

        // GET: api/EmployeesApi/{id}
        /// <summary>
        /// Retrieves a specific employee (User) by their ID. Requires authentication.
        /// </summary>
        [HttpGet("{id}")]
        // Inherits controller auth
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeDto>> GetEmployee(string id)
        {
            // ... implementation unchanged ...
            if (string.IsNullOrEmpty(id)) return BadRequest("Employee ID cannot be null or empty.");
            try
            {
                _logger.LogInformation($"Attempting to retrieve employee with ID: {id}");
                var user = await _context.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == id);
                if (user == null)
                {
                    _logger.LogWarning($"Employee with ID: {id} not found.");
                    return NotFound($"Employee with ID {id} not found.");
                }
                var employeeDto = new EmployeeDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    HireDate = user.HireDate,
                    DepartmentID = user.DepartmentID,
                    DepartmentName = user.Department?.DepartmentName
                };
                _logger.LogInformation($"Successfully retrieved employee with ID: {id}.");
                return Ok(employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving employee with ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred retrieving employee {id}.");
            }
        }


        // POST: api/EmployeesApi
        /// <summary>
        /// Creates a new employee (User). Requires Admin role.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")] // Specific role authorization
        [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<EmployeeDto>> CreateEmployee([FromBody] CreateEmployeeDto createEmployeeDto)
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation($"Attempting to create a new employee with email: {createEmployeeDto.Email}");
                var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentID == createEmployeeDto.DepartmentID);
                if (!departmentExists)
                {
                    _logger.LogWarning($"Department with ID: {createEmployeeDto.DepartmentID} not found during employee creation.");
                    ModelState.AddModelError(nameof(createEmployeeDto.DepartmentID), $"Department with ID {createEmployeeDto.DepartmentID} does not exist.");
                    return BadRequest(ModelState);
                }
                foreach (var roleName in createEmployeeDto.Roles)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        _logger.LogWarning($"Role '{roleName}' not found during employee creation.");
                        ModelState.AddModelError(nameof(createEmployeeDto.Roles), $"Role '{roleName}' does not exist.");
                        return BadRequest(ModelState);
                    }
                }
                var user = new User
                {
                    UserName = createEmployeeDto.Email,
                    Email = createEmployeeDto.Email,
                    FirstName = createEmployeeDto.FirstName,
                    LastName = createEmployeeDto.LastName,
                    PhoneNumber = createEmployeeDto.PhoneNumber,
                    HireDate = createEmployeeDto.HireDate,
                    DepartmentID = createEmployeeDto.DepartmentID,
                    EmailConfirmed = true
                };
                IdentityResult result = await _userManager.CreateAsync(user, createEmployeeDto.Password);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); _logger.LogWarning($"Identity error during employee creation for {createEmployeeDto.Email}: {error.Description}"); }
                    return BadRequest(ModelState);
                }
                _logger.LogInformation($"Successfully created user with ID: {user.Id} for email: {createEmployeeDto.Email}.");
                if (createEmployeeDto.Roles.Any())
                {
                    IdentityResult roleResult = await _userManager.AddToRolesAsync(user, createEmployeeDto.Roles);
                    if (!roleResult.Succeeded) { foreach (var error in roleResult.Errors) { _logger.LogWarning($"Error assigning roles to user {user.Id}: {error.Description}"); } }
                    else { _logger.LogInformation($"Successfully assigned roles {string.Join(",", createEmployeeDto.Roles)} to user {user.Id}."); }
                }
                var employeeDto = new EmployeeDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    HireDate = user.HireDate,
                    DepartmentID = user.DepartmentID,
                    DepartmentName = null
                };
                var dept = await _context.Departments.FindAsync(user.DepartmentID);
                employeeDto.DepartmentName = dept?.DepartmentName;
                return CreatedAtAction(nameof(GetEmployee), new { id = user.Id }, employeeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while creating employee: {createEmployeeDto.Email}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the employee.");
            }
        }


        // PUT: api/EmployeesApi/{id}
        /// <summary>
        /// Updates an existing employee (User). Requires Admin role.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")] // Specific role authorization
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateEmployee(string id, [FromBody] UpdateEmployeeDto updateEmployeeDto)
        {
            // ... implementation unchanged ...
            if (string.IsNullOrEmpty(id)) return BadRequest("Employee ID cannot be null or empty.");
            _logger.LogInformation($"Attempting to update employee with ID: {id}");
            var userToUpdate = await _userManager.FindByIdAsync(id);
            if (userToUpdate == null)
            {
                _logger.LogWarning($"Update failed. Employee with ID: {id} not found.");
                return NotFound($"Employee with ID {id} not found.");
            }
            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentID == updateEmployeeDto.DepartmentID);
            if (!departmentExists)
            {
                _logger.LogWarning($"Department with ID: {updateEmployeeDto.DepartmentID} not found during employee update.");
                ModelState.AddModelError(nameof(updateEmployeeDto.DepartmentID), $"Department with ID {updateEmployeeDto.DepartmentID} does not exist.");
                return BadRequest(ModelState);
            }
            userToUpdate.FirstName = updateEmployeeDto.FirstName;
            userToUpdate.LastName = updateEmployeeDto.LastName;
            userToUpdate.PhoneNumber = updateEmployeeDto.PhoneNumber;
            userToUpdate.HireDate = updateEmployeeDto.HireDate;
            userToUpdate.DepartmentID = updateEmployeeDto.DepartmentID;
            if (!string.Equals(userToUpdate.Email, updateEmployeeDto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByEmailAsync(updateEmployeeDto.Email);
                if (existingUser != null && existingUser.Id != userToUpdate.Id)
                {
                    ModelState.AddModelError(nameof(updateEmployeeDto.Email), "Email address is already in use.");
                    return BadRequest(ModelState);
                }
                _logger.LogInformation($"Attempting email change for user {id} from {userToUpdate.Email} to {updateEmployeeDto.Email}.");
                await _userManager.SetEmailAsync(userToUpdate, updateEmployeeDto.Email);
                await _userManager.SetUserNameAsync(userToUpdate, updateEmployeeDto.Email);
                userToUpdate.EmailConfirmed = false;
                _logger.LogInformation($"User {id} email updated. Email confirmation status set to {userToUpdate.EmailConfirmed}.");
            }
            IdentityResult result = await _userManager.UpdateAsync(userToUpdate);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); _logger.LogWarning($"Identity error during employee update for {id}: {error.Description}"); }
                return BadRequest(ModelState);
            }
            _logger.LogInformation($"Successfully updated base properties for employee with ID: {id}.");
            if (updateEmployeeDto.Roles != null)
            {
                _logger.LogInformation($"Attempting to update roles for user {id}. New roles: {string.Join(",", updateEmployeeDto.Roles)}");
                foreach (var roleName in updateEmployeeDto.Roles)
                {
                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        _logger.LogWarning($"Role '{roleName}' not found during employee role update for {id}.");
                        ModelState.AddModelError(nameof(updateEmployeeDto.Roles), $"Role '{roleName}' does not exist.");
                        return BadRequest(ModelState);
                    }
                }
                var currentRoles = await _userManager.GetRolesAsync(userToUpdate);
                var rolesToRemove = currentRoles.Except(updateEmployeeDto.Roles).ToList();
                var rolesToAdd = updateEmployeeDto.Roles.Except(currentRoles).ToList();
                IdentityResult removeResult = IdentityResult.Success;
                IdentityResult addResult = IdentityResult.Success;
                if (rolesToRemove.Any())
                {
                    removeResult = await _userManager.RemoveFromRolesAsync(userToUpdate, rolesToRemove);
                    if (!removeResult.Succeeded) { _logger.LogWarning($"Failed to remove roles {string.Join(",", rolesToRemove)} from user {id}"); } else { _logger.LogInformation($"Removed roles {string.Join(",", rolesToRemove)} from user {id}."); }
                }
                if (rolesToAdd.Any())
                {
                    addResult = await _userManager.AddToRolesAsync(userToUpdate, rolesToAdd);
                    if (!addResult.Succeeded) { _logger.LogWarning($"Failed to add roles {string.Join(",", rolesToAdd)} to user {id}"); } else { _logger.LogInformation($"Added roles {string.Join(",", rolesToAdd)} to user {id}."); }
                }
                if (!removeResult.Succeeded || !addResult.Succeeded) { _logger.LogWarning($"One or more errors occurred during role update for user {id}."); }
            }
            return NoContent();
        }


        // DELETE: api/EmployeesApi/{id}
        /// <summary>
        /// Deletes an employee (User). Requires Admin role.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Specific role authorization
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteEmployee(string id)
        {
            // ... implementation unchanged ...
            if (string.IsNullOrEmpty(id)) return BadRequest("Employee ID cannot be null or empty.");
            _logger.LogInformation($"Attempting to delete employee with ID: {id}");
            var userToDelete = await _userManager.FindByIdAsync(id);
            if (userToDelete == null)
            {
                _logger.LogWarning($"Delete failed. Employee with ID: {id} not found.");
                return NotFound($"Employee with ID {id} not found.");
            }
            bool isManagingProjects = await _context.Projects.AnyAsync(p => p.ManagerID == id);
            bool hasAssignedTasks = await _context.Tasks.AnyAsync(t => t.AssigneeID == id);
            if (isManagingProjects || hasAssignedTasks)
            {
                string dependencyError = "Cannot delete employee because they are referenced elsewhere:";
                if (isManagingProjects) dependencyError += " Managing one or more projects.";
                if (hasAssignedTasks) dependencyError += " Assigned one or more tasks.";
                _logger.LogWarning($"Delete failed for user {id}. {dependencyError}");
                return BadRequest(dependencyError + " Please reassign responsibilities before deleting.");
            }
            IdentityResult result = await _userManager.DeleteAsync(userToDelete);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) { ModelState.AddModelError(string.Empty, error.Description); _logger.LogWarning($"Identity error during employee deletion for {id}: {error.Description}"); }
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the employee.");
            }
            _logger.LogInformation($"Successfully deleted employee with ID: {id}.");
            return NoContent();
        }
    }
}