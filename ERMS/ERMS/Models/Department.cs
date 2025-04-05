// Department.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ERMS.Models
{
    /// <summary>
    /// Represents a department in the Enterprise Resource Management System.
    /// </summary>
    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        [StringLength(50)]
        public string DepartmentName { get; set; }

        // Navigation property
        public ICollection<User> Users { get; set; }
    }
}
