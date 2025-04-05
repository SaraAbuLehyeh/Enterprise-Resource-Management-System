using ERMS.Data;
using ERMS.DTOs;
using ERMS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer; // Required
using Microsoft.AspNetCore.Authorization;          // Required
using Microsoft.AspNetCore.Http;
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
    /// API Controller for managing Task (ProjectTask) resources. (Secured)
    /// Provides CRUD operations for tasks and specific task queries, requires JWT authentication.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // Require JWT for all actions
    public class TasksApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TasksApiController> _logger;

        public TasksApiController(ApplicationDbContext context, ILogger<TasksApiController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/TasksApi
        /// <summary>
        /// Retrieves a list of all tasks. Requires authentication.
        /// </summary>
        [HttpGet]
        // Inherits controller-level authorization
        [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks()
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation("Attempting to retrieve all tasks.");
                var tasks = await _context.Tasks
                    .Include(t => t.Assignee).Include(t => t.Project)
                    .OrderBy(t => t.DueDate).ToListAsync();
                var taskDtos = tasks.Select(t => new TaskDto
                {
                    TaskID = t.TaskID,
                    TaskName = t.TaskName,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    AssigneeID = t.AssigneeID,
                    AssigneeName = t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : "N/A",
                    ProjectID = t.ProjectID,
                    ProjectName = t.Project?.ProjectName
                }).ToList();
                _logger.LogInformation($"Successfully retrieved {taskDtos.Count} tasks.");
                return Ok(taskDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving tasks.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred retrieving tasks.");
            }
        }

        // GET: api/TasksApi/{id}
        /// <summary>
        /// Retrieves a specific task by its ID. Requires authentication.
        /// </summary>
        [HttpGet("{id}")]
        // Inherits controller-level authorization
        [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation($"Attempting to retrieve task with ID: {id}");
                var task = await _context.Tasks
                    .Include(t => t.Assignee).Include(t => t.Project)
                    .FirstOrDefaultAsync(t => t.TaskID == id);
                if (task == null)
                {
                    _logger.LogWarning($"Task with ID: {id} not found.");
                    return NotFound($"Task with ID {id} not found.");
                }
                var taskDto = new TaskDto
                {
                    TaskID = task.TaskID,
                    TaskName = task.TaskName,
                    Description = task.Description,
                    Priority = task.Priority,
                    Status = task.Status,
                    DueDate = task.DueDate,
                    AssigneeID = task.AssigneeID,
                    AssigneeName = task.Assignee != null ? $"{task.Assignee.FirstName} {task.Assignee.LastName}" : "N/A",
                    ProjectID = task.ProjectID,
                    ProjectName = task.Project?.ProjectName
                };
                _logger.LogInformation($"Successfully retrieved task with ID: {id}.");
                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving task with ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred retrieving task {id}.");
            }
        }

        // GET: api/TasksApi/user/{userId}
        /// <summary>
        /// Retrieves all tasks assigned to a specific user. Requires authentication.
        /// </summary>
        [HttpGet("user/{userId}")]
        // Inherits controller-level authorization. Consider adding policy for self/manager access.
        [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByUser(string userId)
        {
            // ... implementation unchanged ...
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID cannot be null or empty.");
            try
            {
                _logger.LogInformation($"Attempting to retrieve tasks for user ID: {userId}");
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                if (!userExists)
                {
                    _logger.LogWarning($"User with ID: {userId} not found when retrieving tasks.");
                    return NotFound($"User with ID {userId} not found.");
                }
                var tasks = await _context.Tasks
                   .Include(t => t.Project)
                   .Where(t => t.AssigneeID == userId)
                   .OrderBy(t => t.DueDate).ToListAsync();
                var taskDtos = tasks.Select(t => new TaskDto
                {
                    TaskID = t.TaskID,
                    TaskName = t.TaskName,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    AssigneeID = t.AssigneeID,
                    AssigneeName = null, // Name not loaded, could add if needed
                    ProjectID = t.ProjectID,
                    ProjectName = t.Project?.ProjectName
                }).ToList();
                _logger.LogInformation($"Successfully retrieved {taskDtos.Count} tasks for user ID: {userId}.");
                return Ok(taskDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving tasks for user ID: {userId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred retrieving tasks for user {userId}.");
            }
        }


        // POST: api/TasksApi
        /// <summary>
        /// Creates a new task. Requires Admin or Manager role.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")] // Specific role authorization
        [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            // ... implementation unchanged ...
            try
            {
                _logger.LogInformation($"Attempting to create task: {createTaskDto.TaskName}");
                var projectExists = await _context.Projects.AnyAsync(p => p.ProjectID == createTaskDto.ProjectID);
                if (!projectExists)
                {
                    _logger.LogWarning($"Project with ID: {createTaskDto.ProjectID} not found during task creation.");
                    ModelState.AddModelError(nameof(createTaskDto.ProjectID), $"Project with ID {createTaskDto.ProjectID} does not exist.");
                    return BadRequest(ModelState);
                }
                var assigneeExists = await _context.Users.AnyAsync(u => u.Id == createTaskDto.AssigneeID);
                if (!assigneeExists)
                {
                    _logger.LogWarning($"Assignee with ID: {createTaskDto.AssigneeID} not found during task creation.");
                    ModelState.AddModelError(nameof(createTaskDto.AssigneeID), $"Assignee with ID {createTaskDto.AssigneeID} does not exist.");
                    return BadRequest(ModelState);
                }
                var task = new ProjectTask
                {
                    TaskName = createTaskDto.TaskName,
                    Description = createTaskDto.Description,
                    Priority = createTaskDto.Priority,
                    Status = createTaskDto.Status,
                    DueDate = createTaskDto.DueDate,
                    AssigneeID = createTaskDto.AssigneeID,
                    ProjectID = createTaskDto.ProjectID
                };
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully created task with ID: {task.TaskID}.");
                var taskDto = new TaskDto
                {
                    TaskID = task.TaskID,
                    TaskName = task.TaskName,
                    Description = task.Description,
                    Priority = task.Priority,
                    Status = task.Status,
                    DueDate = task.DueDate,
                    AssigneeID = task.AssigneeID,
                    ProjectID = task.ProjectID,
                    AssigneeName = null,
                    ProjectName = null
                };
                var assignee = await _context.Users.FindAsync(task.AssigneeID);
                var project = await _context.Projects.FindAsync(task.ProjectID);
                taskDto.AssigneeName = assignee != null ? $"{assignee.FirstName} {assignee.LastName}" : "N/A";
                taskDto.ProjectName = project?.ProjectName;
                return CreatedAtAction(nameof(GetTask), new { id = task.TaskID }, taskDto);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error occurred while creating task: {createTaskDto.TaskName}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "A database error occurred while creating the task.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while creating task: {createTaskDto.TaskName}.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred while creating the task.");
            }
        }


        // PUT: api/TasksApi/{id}
        /// <summary>
        /// Updates an existing task. Requires Admin or Manager role.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")] // Specific role authorization
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskDto updateTaskDto)
        {
            // ... implementation unchanged ...
            _logger.LogInformation($"Attempting to update task with ID: {id}");
            var taskToUpdate = await _context.Tasks.FindAsync(id);
            if (taskToUpdate == null)
            {
                _logger.LogWarning($"Update failed. Task with ID: {id} not found.");
                return NotFound($"Task with ID {id} not found.");
            }
            var projectExists = await _context.Projects.AnyAsync(p => p.ProjectID == updateTaskDto.ProjectID);
            if (!projectExists)
            {
                _logger.LogWarning($"Project with ID: {updateTaskDto.ProjectID} not found during task update (ID: {id}).");
                ModelState.AddModelError(nameof(updateTaskDto.ProjectID), $"Project with ID {updateTaskDto.ProjectID} does not exist.");
                return BadRequest(ModelState);
            }
            var assigneeExists = await _context.Users.AnyAsync(u => u.Id == updateTaskDto.AssigneeID);
            if (!assigneeExists)
            {
                _logger.LogWarning($"Assignee with ID: {updateTaskDto.AssigneeID} not found during task update (ID: {id}).");
                ModelState.AddModelError(nameof(updateTaskDto.AssigneeID), $"Assignee with ID {updateTaskDto.AssigneeID} does not exist.");
                return BadRequest(ModelState);
            }
            taskToUpdate.TaskName = updateTaskDto.TaskName; taskToUpdate.Description = updateTaskDto.Description;
            taskToUpdate.Priority = updateTaskDto.Priority; taskToUpdate.Status = updateTaskDto.Status;
            taskToUpdate.DueDate = updateTaskDto.DueDate; taskToUpdate.AssigneeID = updateTaskDto.AssigneeID;
            taskToUpdate.ProjectID = updateTaskDto.ProjectID;
            _context.Entry(taskToUpdate).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully updated task with ID: {id}.");
            }
            catch (DbUpdateConcurrencyException concEx)
            {
                _logger.LogWarning(concEx, $"Concurrency conflict while updating task ID: {id}.");
                return Conflict($"Concurrency conflict updating task {id}.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error occurred while updating task ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"A database error occurred while updating task {id}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating task ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred updating task {id}.");
            }
            return NoContent();
        }

        // PATCH: api/TasksApi/{id}/status
        /// <summary>
        /// Updates the status of a specific task. Requires authentication.
        /// </summary>
        [HttpPatch("{id}/status")]
        // Inherits controller-level authorization. Consider restricting to Admin/Manager/Assignee.
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)] // If role/policy added later
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusDto updateStatusDto)
        {
            // ... implementation unchanged ...
            _logger.LogInformation($"Attempting to update status for task ID: {id} to '{updateStatusDto.Status}'");
            var taskToUpdate = await _context.Tasks.FindAsync(id);
            if (taskToUpdate == null)
            {
                _logger.LogWarning($"Status update failed. Task with ID: {id} not found.");
                return NotFound($"Task with ID {id} not found.");
            }
            taskToUpdate.Status = updateStatusDto.Status;
            _context.Entry(taskToUpdate).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully updated status for task ID: {id}.");
            }
            catch (DbUpdateConcurrencyException concEx)
            {
                _logger.LogWarning(concEx, $"Concurrency conflict while updating status for task ID: {id}.");
                return Conflict($"Concurrency conflict updating task {id}.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error occurred while updating status for task ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"A database error occurred while updating status for task {id}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while updating status for task ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred updating status for task {id}.");
            }
            return Ok($"Status for task {id} updated successfully.");
        }
        // GET: api/TasksApi/project/{projectId}
        [HttpGet("project/{projectId}")] // Define specific route
        // Inherits controller-level JWT authorization [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [ProducesResponseType(typeof(IEnumerable<TaskDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)] // If project itself doesn't exist
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasksByProject(int projectId)
        {
            _logger.LogInformation("Attempting to retrieve tasks for Project ID: {ProjectId}", projectId);

            try
            {
                // Optional but good practice: Check if the project actually exists first
                var projectExists = await _context.Projects.AnyAsync(p => p.ProjectID == projectId);
                if (!projectExists)
                {
                    _logger.LogWarning("Project with ID {ProjectId} not found when retrieving its tasks.", projectId);
                    // Return 404 if the parent project doesn't exist
                    return NotFound($"Project with ID {projectId} not found.");
                }

                // Query tasks specifically for the given project ID
                var tasks = await _context.Tasks
                   .Where(t => t.ProjectID == projectId) // Filter by projectId
                   .Include(t => t.Assignee) // Include Assignee details
                                             // No need to Include Project as we already know the ProjectID
                   .OrderBy(t => t.DueDate) // Order as desired
                   .ToListAsync();

                // Map results to DTOs
                var taskDtos = tasks.Select(t => new TaskDto
                {
                    TaskID = t.TaskID,
                    TaskName = t.TaskName,
                    Description = t.Description,
                    Priority = t.Priority,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    AssigneeID = t.AssigneeID,
                    AssigneeName = t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : "N/A",
                    ProjectID = t.ProjectID, // Include ProjectID in DTO
                    ProjectName = null // Project Name could be added if needed by joining or separate lookup
                }).ToList();

                _logger.LogInformation("Successfully retrieved {TaskCount} tasks for Project ID: {ProjectId}.", taskDtos.Count, projectId);
                return Ok(taskDtos); // Return the list (even if empty)
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving tasks for Project ID: {ProjectId}", projectId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred retrieving tasks for project {projectId}.");
            }
        }
        // *** END OF ADDED METHOD ***

        // DELETE: api/TasksApi/{id}
        /// <summary>
        /// Deletes a specific task. Requires Admin or Manager role.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")] // Specific role authorization
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteTask(int id)
        {
            // ... implementation unchanged ...
            _logger.LogInformation($"Attempting to delete task with ID: {id}");
            var taskToDelete = await _context.Tasks.FindAsync(id);
            if (taskToDelete == null)
            {
                _logger.LogWarning($"Delete failed. Task with ID: {id} not found.");
                return NotFound($"Task with ID {id} not found.");
            }
            try
            {
                _context.Tasks.Remove(taskToDelete);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Successfully deleted task with ID: {id}.");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, $"Database error occurred while deleting task ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"A database error occurred while deleting task {id}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting task ID: {id}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred deleting task {id}.");
            }
            return NoContent();
        }
    }
}