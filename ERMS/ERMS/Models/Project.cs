// Project.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERMS.Models
{
    /// <summary>
    /// Represents a project in the Enterprise Resource Management System.
    /// </summary>
    public class Project
    {
        [Key]
        public int ProjectID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProjectName { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string ManagerID { get; set; } // Changé de int à string

        // Navigation properties
        [ForeignKey("ManagerID")]
        public User Manager { get; set; }

        public ICollection<ProjectTask> Tasks { get; set; }
    }
}
