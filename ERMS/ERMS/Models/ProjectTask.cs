// ProjectTask.cs (renamed from Task.cs to avoid conflict with System.Threading.Tasks.Task)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERMS.Models
{
    /// <summary>
    /// Represents a task in the Enterprise Resource Management System.
    /// </summary>
    public class ProjectTask
    {
        [Key]
        public int TaskID { get; set; }

        public int ProjectID { get; set; }

        public string AssigneeID { get; set; }

        [Required]
        [StringLength(100)]
        public string TaskName { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [StringLength(50)]
        public string Priority { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Not Started";

        // Navigation properties
        [ForeignKey("ProjectID")]
        public Project Project { get; set; }

        [ForeignKey("AssigneeID")]
        public User Assignee { get; set; }
    }
}
