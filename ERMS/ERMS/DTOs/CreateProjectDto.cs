using System.ComponentModel.DataAnnotations;

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new Project.
    /// Used as the request body for the POST endpoint.
    /// </summary>
    public class CreateProjectDto
    {
        /// <summary>
        /// Name of the project.
        /// </summary>
        /// <example>New Marketing Campaign</example>
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(100, ErrorMessage = "Project name cannot exceed 100 characters.")]
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the project.
        /// </summary>
        /// <example>Plan and execute the Q4 marketing campaign.</example>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Date when the project is scheduled to start.
        /// </summary>
        /// <example>2024-10-01</example>
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Optional date when the project is scheduled to end.
        /// </summary>
        /// <example>2025-01-31</example>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Identifier of the user who will manage the project.
        /// </summary>
        /// <example>auth0|60d...</example> // Example user ID format
        [Required(ErrorMessage = "Manager ID is required.")]
        public string ManagerID { get; set; } = string.Empty;
    }
}