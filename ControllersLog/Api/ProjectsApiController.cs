using ERMS.Data;
using ERMS.DTOs;
using ERMS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Required for scheme constant
using Microsoft.AspNetCore.Authorization;          // Required for Authorize attribute
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERMS.Controllers.Api
{
    /// <summary>
    /// API Controller for managing Project resources. (Secured)
    /// Provides CRUD operations for projects, requiring JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Require JWT for all actions in this controller
    public class ProjectsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectsApiController> _logger;

        public ProjectsApiController(ApplicationDbContext context, ILogger<ProjectsApiController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/ProjectsApi
        /// <summary>
        /// Retrieves a list of all projects. Requires authentication.
        /// </summary>
        /*[HttpGet]
        // Inherits controller-level authorization
        [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]*/
        [HttpGet]
        [AllowAnonymous] // <-- TEMPORARILY add this for HttpClient demo
        [ProducesResponseType(typeof(IEnumerable<ProjectDto>), StatusCodes.Status200OK)]
        // No 401/403 needed when AllowAnonymous is present
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation("Attempting to retrieve all projects.");
                var projects = await _context.Projects
                    .Include(p => p.Manager).Include(p => p.Tasks)
                    .OrderBy(p => p.ProjectName).ToListAsync();
                var projectDtos = projects.Select(p => new ProjectDto
                {
                    ProjectID = p.ProjectID,
                    ProjectName = p.ProjectName,
                    Description = p.Description,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    ManagerID = p.ManagerID,
                    ManagerName = p.Manager != null ? $"{p.Manager.FirstName} {p.Manager.LastName}" : "N/A",
                    TaskCount = p.Tasks?.Count ?? 0
                }).ToList();
                _logger.LogInformation($"Successfully retrieved {projectDtos.Count} projects.");
                return Ok(projectDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving projects.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred retrieving projects.");
            }
        }

        // GET: api/ProjectsApi/5
        /// <summary>
        /// Retrieves a specific project by its ID. Requires authentication.
        /// </summary>
        /*[HttpGet("{id}")]
        // Inherits controller-level authorization
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]*/
        [HttpGet("{id}")]
        [AllowAnonymous] // <-- TEMPORARILY add this for HttpClient demo
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
        // No 401/403 needed when AllowAnonymous is present
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation($"Attempting to retrieve project with ID: {id}");
                var project = await _context.Projects
                    .Include(p => p.Manager).Include(p => p.Tasks)
                    .FirstOrDefaultAsync(p => p.ProjectID == id);
                if (project == null)
                {
                    _logger.LogWarning($"Project with ID: {id} not found.");
                    return NotFound($"Project with ID {id} not found.");
                }
                var projectDto = new ProjectDto
                {
                    ProjectID = project.ProjectID,
                    ProjectName = project.ProjectName,
                    Description = project.Description,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                    ManagerID = project.ManagerID,
                    ManagerName = project.Manager != null ? $"{project.Manager.FirstName} {project.Manager.LastName}" : "N/A",
                    TaskCount = project.Tasks?.Count ?? 0
                };
                _logger.LogInformation($"Successfully retrieved project with ID: {id}.");
                return Ok(projectDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving project with ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred retrieving project {id}.");
            }
        }

        // POST: api/ProjectsApi
        /// <summary>
        /// Creates a new project. Requires Admin or Manager role.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")] // Specific role authorization
        [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProjectDto>> CreateProject([FromBody] CreateProjectDto createProjectDto)
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation($"Attempting to create a new project with name: {createProjectDto.ProjectName}");
                var managerExists = await _context.Users.AnyAsync(u => u.Id == createProjectDto.ManagerID);
                if (!managerExists)
                {
                    _logger.LogWarning($"Manager with ID: {createProjectDto.ManagerID} not found during project creation.");
                    ModelState.AddModelError(nameof(createProjectDto.ManagerID), $"Manager with ID {createProjectDto.ManagerID} does not exist.");
                    return BadRequest(ModelState);
                }
                var project = new Project
                {
                    ProjectName = createProjectDto.ProjectName,
                    Description = createProjectDto.Description,
                    StartDate = createProjectDto.StartDate,
                    EndDate = createProjectDto.EndDate,
                    ManagerID = createProjectDto.ManagerID
                };
                _context.Projects.Add(project);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully created project with ID: {project.ProjectID}.");
                var projectDto = new ProjectDto
                {
                    ProjectID = project.ProjectID,
                    ProjectName = project.ProjectName,
                    Description = project.Description,
                    StartDate = project.StartDate,
                    EndDate = project.EndDate,
                    ManagerID = project.ManagerID,
                    ManagerName = null,
                    TaskCount = 0
                };
                return CreatedAtAction(nameof(GetProject), new { id = project.ProjectID }, projectDto);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error occurred while creating project: {createProjectDto.ProjectName}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred while creating the project.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while creating project: {createProjectDto.ProjectName}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the project.");
            }
        }

        // PUT: api/ProjectsApi/5
        /// <summary>
        /// Updates an existing project. Requires Admin or Manager role.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")] // Specific role authorization
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto updateProjectDto)
        {
            // ... implementation unchanged ...
            _logger.LogInformation($"Attempting to update project with ID: {id}");
            var projectToUpdate = await _context.Projects.FindAsync(id);
            if (projectToUpdate == null)
            {
                _logger.LogWarning($"Update failed. Project with ID: {id} not found.");
                return NotFound($"Project with ID {id} not found.");
            }
            var managerExists = await _context.Users.AnyAsync(u => u.Id == updateProjectDto.ManagerID);
            if (!managerExists)
            {
                _logger.LogWarning($"Manager with ID: {updateProjectDto.ManagerID} not found during project update (ID: {id}).");
                ModelState.AddModelError(nameof(updateProjectDto.ManagerID), $"Manager with ID {updateProjectDto.ManagerID} does not exist.");
                return BadRequest(ModelState);
            }
            projectToUpdate.ProjectName = updateProjectDto.ProjectName;
            projectToUpdate.Description = updateProjectDto.Description;
            projectToUpdate.StartDate = updateProjectDto.StartDate;
            projectToUpdate.EndDate = updateProjectDto.EndDate;
            projectToUpdate.ManagerID = updateProjectDto.ManagerID;
            _context.Entry(projectToUpdate).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully updated project with ID: {id}.");
            }
            catch (DbUpdateConcurrencyException concEx)
            {
                _logger.LogWarning(concEx, $"Concurrency conflict while updating project ID: {id}.");
                return Conflict($"Concurrency conflict updating project {id}.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error occurred while updating project ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"A database error occurred while updating project {id}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating project ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred updating project {id}.");
            }
            return NoContent();
        }

        // DELETE: api/ProjectsApi/5
        /// <summary>
        /// Deletes a specific project. Requires Admin role.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // Specific role authorization (Admin only)
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProject(int id)
        {
            // ... implementation unchanged ...
            _logger.LogInformation($"Attempting to delete project with ID: {id}");
            var projectToDelete = await _context.Projects.FindAsync(id);
            if (projectToDelete == null)
            {
                _logger.LogWarning($"Delete failed. Project with ID: {id} not found.");
                return NotFound($"Project with ID {id} not found.");
            }
            var hasTasks = await _context.Tasks.AnyAsync(t => t.ProjectID == id);
            if (hasTasks)
            {
                _logger.LogWarning($"Delete failed. Project with ID: {id} has associated tasks.");
                return BadRequest($"Cannot delete project {id} because it has associated tasks. Please reassign or delete tasks first.");
            }
            try
            {
                _context.Projects.Remove(projectToDelete);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully deleted project with ID: {id}.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error occurred while deleting project ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"A database error occurred while deleting project {id}. Check for dependencies.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting project ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred deleting project {id}.");
            }
            return NoContent();
        }

        // Helper method (example, might not be needed)
        private async Task<bool> ProjectExists(int id)
        {
            return await _context.Projects.AnyAsync(e => e.ProjectID == id);
        }
    }
}