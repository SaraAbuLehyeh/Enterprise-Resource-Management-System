using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMS.DTOs
{
    /// <summary>
    /// Data Transfer Object for updating an existing Employee (User).
    /// Note: Password changes should typically have a separate, dedicated endpoint/process.
    /// </summary>
    public class UpdateEmployeeDto
    {
        // User ID passed via route

        /// <summary>
        /// Employee's first name.
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Employee's last name.
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Employee's email address. Must be unique if changed.
        /// </summary>
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Employee's phone number (optional).
        /// </summary>
        public string? PhoneNumber { get; set; } // Remains nullable

        /// <summary>
        /// Date the employee was hired. This field is required.
        /// </summary>
        [Required(ErrorMessage = "Hire date is required.")]
        public DateTime HireDate { get; set; } // Non-nullable DateTime

        /// <summary>
        /// ID of the department the employee belongs to. This field is required.
        /// </summary>
        [Required(ErrorMessage = "Department ID is required.")]
        public int DepartmentID { get; set; } // Non-nullable int

        /// <summary>
        /// List of roles to assign to the employee. Updating roles might overwrite existing ones.
        /// Can be null if roles are not being updated, or empty list to remove all roles.
        /// </summary>
        /// <example>["Employee"]</example>
        public List<string>? Roles { get; set; } // Keep as nullable for flexibility in PUT
    }
}