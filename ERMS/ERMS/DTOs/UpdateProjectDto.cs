using System.ComponentModel.DataAnnotations;

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating an existing Project.
    /// Used as the request body for the PUT endpoint.
    /// </summary>
    public class UpdateProjectDto
    {
        // Note: ProjectID is typically passed via the URL route, not in the body for PUT.

        /// <summary>
        /// Name of the project.
        /// </summary>
        [Required(ErrorMessage = "Project name is required.")]
        [StringLength(100, ErrorMessage = "Project name cannot exceed 100 characters.")]
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the project.
        /// </summary>
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        public string? Description { get; set; }

        /// <summary>
        /// Date when the project is scheduled to start.
        /// </summary>
        [Required(ErrorMessage = "Start date is required.")]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Optional date when the project is scheduled to end.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Identifier of the user who will manage the project.
        /// </summary>
        [Required(ErrorMessage = "Manager ID is required.")]
        public string ManagerID { get; set; } = string.Empty;
    }
}