using System.ComponentModel.DataAnnotations;

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object specifically for updating a Task's status via PATCH.
    /// </summary>
    public class UpdateTaskStatusDto
    {
        /// <summary>
        /// The new status for the task as a string.
        /// </summary>
        /// <example>Completed</example>
        [Required(ErrorMessage = "Status is required.")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        public string Status { get; set; } = string.Empty; // string type
    }
}