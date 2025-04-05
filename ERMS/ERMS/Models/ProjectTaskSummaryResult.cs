// File: Models/ProjectTaskSummaryResult.cs (or similar namespace)
using System;

namespace ERMS.Models // Or ViewModels/StoredProcedureResults
{
    // Represents the result set returned by spGetProjectTaskSummary
    // IMPORTANT: Property names MUST match column names returned by the SP
    public class ProjectTaskSummaryResult
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; } // Match SP output
        public string ManagerName { get; set; } = string.Empty;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
    }
}