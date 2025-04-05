using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for creating a new Employee (User).
    /// Includes necessary fields for Identity user creation.
    /// </summary>
    public class CreateEmployeeDto
    {
        /// <summary>
        /// Employee's first name.
        /// </summary>
        /// <example>Jane</example>
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Employee's last name.
        /// </summary>
        /// <example>Smith</example>
        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Employee's email address. Must be unique.
        /// </summary>
        /// <example>jane.smith@example.com</example>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Employee's initial password. Must meet complexity requirements.
        /// </summary>
        /// <example>Password123!</example>
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        // Consider adding [MinLength] or specific regex if needed, complementing Program.cs setup
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Employee's phone number (optional).
        /// </summary>
        /// <example>555-987-6543</example>
        public string? PhoneNumber { get; set; } // Remains nullable as it's not required in User model

        /// <summary>
        /// Date the employee was hired. This field is required.
        /// </summary>
        /// <example>2024-01-10T00:00:00Z</example>
        [Required(ErrorMessage = "Hire date is required.")]
        public DateTime HireDate { get; set; } // Non-nullable DateTime

        /// <summary>
        /// ID of the department the employee belongs to. This field is required.
        /// </summary>
        /// <example>1</example>
        [Required(ErrorMessage = "Department ID is required.")]
        public int DepartmentID { get; set; } // Non-nullable int

        /// <summary>
        /// List of roles to assign to the new employee. Must match existing role names.
        /// </summary>
        /// <example>["Employee", "Manager"]</example>
        [Required(ErrorMessage = "At least one role must be assigned.")] // Make roles required? Optional.
        [MinLength(1, ErrorMessage = "At least one role must be selected.")]
        public List<string> Roles { get; set; } = new List<string>();
    }
}