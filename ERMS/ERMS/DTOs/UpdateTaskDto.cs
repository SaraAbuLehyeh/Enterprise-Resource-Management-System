using System.ComponentModel.DataAnnotations;

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating an existing Task (ProjectTask).
    /// </summary>
    public class UpdateTaskDto
    {
        // TaskID passed via route

        /// <summary>
        /// Name of the task.
        /// </summary>
        [Required(ErrorMessage = "Task name is required.")]
        [StringLength(100, ErrorMessage = "Task name cannot exceed 100 characters.")]
        public string TaskName { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the task.
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Priority level of the task as a string.
        /// </summary>
        [Required(ErrorMessage = "Priority is required.")]
        [StringLength(50, ErrorMessage = "Priority cannot exceed 50 characters.")]
        public string Priority { get; set; } = string.Empty; // string type

        /// <summary>
        /// Status of the task as a string. Use the dedicated PATCH endpoint for status-only updates usually.
        /// </summary>
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = string.Empty; // string type

        /// <summary>
        /// Date when the task is due.
        /// </summary>
        [Required(ErrorMessage = "Due date is required.")]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Identifier of the user assigned to the task.
        /// </summary>
        [Required(ErrorMessage = "Assignee ID is required.")]
        public string AssigneeID { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the project this task belongs to.
        /// </summary>
        [Required(ErrorMessage = "Project ID is required.")]
        public int ProjectID { get; set; }
    }
}