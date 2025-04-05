using System.ComponentModel.DataAnnotations;
// No longer need using ERMS.Models for enums here

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for representing a Task (ProjectTask).
    /// Used for returning task details via the API.
    /// </summary>
    public class TaskDto
    {
        /// <summary>
        /// Unique identifier for the task.
        /// </summary>
        /// <example>501</example>
        public int TaskID { get; set; }

        /// <summary>
        /// Name of the task.
        /// </summary>
        /// <example>Design Homepage Mockup</example>
        [Required]
        [StringLength(100)]
        public string TaskName { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the task.
        /// </summary>
        /// <example>Create Figma mockups for the new website homepage.</example>
        public string? Description { get; set; }

        /// <summary>
        /// Priority level of the task as a string (e.g., "Low", "Medium", "High").
        /// </summary>
        /// <example>High</example>
        [Required]
        [StringLength(50)] // Match model's StringLength
        public string Priority { get; set; } = string.Empty; // string type

        /// <summary>
        /// Current status of the task as a string (e.g., "Not Started", "In Progress", "Completed").
        /// </summary>
        /// <example>In Progress</example>
        [Required]
        [StringLength(50)] // Match model's StringLength
        public string Status { get; set; } = string.Empty; // string type

        /// <summary>
        /// Date when the task is due.
        /// </summary>
        /// <example>2024-08-15T00:00:00Z</example>
        [Required]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Identifier of the user assigned to the task.
        /// </summary>
        /// <example>auth0|60e...</example>
        [Required]
        public string AssigneeID { get; set; } = string.Empty;

        /// <summary>
        /// Name of the assignee (read-only convenience field).
        /// </summary>
        /// <example>Bob Johnson</example>
        public string? AssigneeName { get; set; } // Populated during mapping

        /// <summary>
        /// Identifier of the project this task belongs to.
        /// </summary>
        /// <example>101</example>
        [Required]
        public int ProjectID { get; set; }

        /// <summary>
        /// Name of the project this task belongs to (read-only convenience field).
        /// </summary>
        /// <example>New Website Launch</example>
        public string? ProjectName { get; set; } // Populated during mapping
    }
}