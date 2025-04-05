using System.ComponentModel.DataAnnotations;

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for representing an Employee.
    /// Combines data potentially from User and a specific Employee profile table.
    /// </summary>
    public class EmployeeDto
    {
        /// <summary>
        /// The unique identifier of the User identity.
        /// </summary>
        /// <example>auth0|5fc...</example>
        [Required]
        public string Id { get; set; } = string.Empty; // Corresponds to User.Id

        /// <summary>
        /// Employee's first name.
        /// </summary>
        /// <example>John</example>
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty; // Likely comes from User

        /// <summary>
        /// Employee's last name.
        /// </summary>
        /// <example>Doe</example>
        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty; // Likely comes from User

        /// <summary>
        /// Employee's email address (unique).
        /// </summary>
        /// <example>john.doe@example.com</example>
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty; // Comes from User

        /// <summary>
        /// Employee's phone number.
        /// </summary>
        /// <example>+1-555-123-4567</example>
        public string? PhoneNumber { get; set; } // Comes from User

        /// <summary>
        /// Date the employee was hired.
        /// </summary>
        /// <example>2023-05-15T00:00:00Z</example>
        public DateTime? HireDate { get; set; } // May come from User or a separate Employee profile table

        /// <summary>
        /// ID of the department the employee belongs to.
        /// </summary>
        /// <example>3</example>
        public int? DepartmentID { get; set; } // Comes from User

        /// <summary>
        /// Name of the department (read-only convenience field).
        /// </summary>
        /// <example>Engineering</example>
        public string? DepartmentName { get; set; } // Populated during mapping
    }
}