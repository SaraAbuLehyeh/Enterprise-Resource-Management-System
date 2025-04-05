using System.ComponentModel.DataAnnotations; // Required for validation attributes if needed later

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for representing a Project.
    /// Used for returning project details via the API.
    /// </summary>
    public class ProjectDto
    {
        /// <summary>
        /// Unique identifier for the project.
        /// </summary>
        /// <example>101</example>
        public int ProjectID { get; set; }

        /// <summary>
        /// Name of the project.
        /// </summary>
        /// <example>New Website Launch</example>
        [Required] // Even for read DTOs, helps indicate non-nullability
        [StringLength(100)]
        public string ProjectName { get; set; } = string.Empty; // Initialize to avoid null warnings

        /// <summary>
        /// Detailed description of the project.
        /// </summary>
        /// <example>Launch the new corporate website by Q3.</example>
        public string? Description { get; set; } // Nullable if description is optional

        /// <summary>
        /// Date when the project is scheduled to start.
        /// </summary>
        /// <example>2024-08-01T00:00:00Z</example>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Date when the project is scheduled to end (optional).
        /// </summary>
        /// <example>2025-03-31T00:00:00Z</example>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Identifier of the user managing the project.
        /// </summary>
        /// <example>auth0|60d...</example> // Example user ID format
        [Required]
        public string ManagerID { get; set; } = string.Empty;

        /// <summary>
        /// Name of the manager assigned to the project (read-only convenience field).
        /// </summary>
        /// <example>Alice Smith</example>
        public string? ManagerName { get; set; } // Populated during mapping

        /// <summary>
        /// Number of tasks associated with the project (read-only convenience field).
        /// </summary>
        /// <example>5</example>
        public int TaskCount { get; set; } // Populated during mapping

        // Consider adding a list of Task DTOs if needed, but be mindful of payload size.
        // public List<TaskSummaryDto> Tasks { get; set; } = new List<TaskSummaryDto>();
    }
}