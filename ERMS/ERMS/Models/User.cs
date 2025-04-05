using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERMS.Models
{
    /// <summary>
    /// Represents a user in the Enterprise Resource Management System.
    /// Extends the IdentityUser class to include additional properties.
    /// </summary>
    public class User : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        public DateTime HireDate { get; set; }

        public int DepartmentID { get; set; }

        // Navigation properties
        [ForeignKey("DepartmentID")]
        public Department Department { get; set; }

        public ICollection<Project> ManagedProjects { get; set; }

        public ICollection<ProjectTask> AssignedTasks { get; set; }
    }
}
