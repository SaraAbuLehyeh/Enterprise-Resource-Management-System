using ERMS.Data;
using ERMS.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace ERMS.Controllers
{
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> ProjectSummary(DateTime? startDate, DateTime? endDate, int? departmentId)
        {
            IQueryable<Project> query = _context.Projects
                .Include(p => p.Tasks)
                .Include(p => p.Manager)
                    .ThenInclude(m => m.Department);

            if (startDate.HasValue)
                query = query.Where(p => p.StartDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.EndDate <= endDate.Value || p.EndDate == null);

            if (departmentId.HasValue)
                query = query.Where(p => p.Manager.DepartmentID == departmentId.Value);

            var projects = await query.ToListAsync();

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.DepartmentId = departmentId;

            return View(projects);
        }

        public async Task<IActionResult> EmployeePerformance(DateTime? startDate, DateTime? endDate, int? departmentId)
        {
            IQueryable<User> query = _context.Users
                .Include(u => u.Department)
                .Include(u => u.AssignedTasks);

            if (startDate.HasValue)
                query = query.Where(u => u.AssignedTasks.Any(t => t.DueDate >= startDate.Value));

            if (endDate.HasValue)
                query = query.Where(u => u.AssignedTasks.Any(t => t.DueDate <= endDate.Value));

            if (departmentId.HasValue)
                query = query.Where(u => u.DepartmentID == departmentId.Value);

            var employees = await query.ToListAsync();

            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.DepartmentId = departmentId;

            return View(employees);
        }

        // You can add more report types here as needed
        public IActionResult ExportProjectSummaryPdf()
        {
            var projects = _context.Projects.Include(p => p.Tasks).Include(p => p.Manager).ToList();

            using (MemoryStream ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Add header
                Paragraph header = new Paragraph("Project Summary Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20);
                document.Add(header);
                document.Add(new Paragraph("\n"));

                // Create table
                Table table = new Table(4).UseAllAvailableWidth();

                // Add table headers
                table.AddHeaderCell("Project Name");
                table.AddHeaderCell("Manager");
                table.AddHeaderCell("Total Tasks");
                table.AddHeaderCell("Completed Tasks");

                // Add table data
                foreach (var project in projects)
                {
                    table.AddCell(project.ProjectName);
                    table.AddCell(project.Manager != null ? $"{project.Manager.FirstName} {project.Manager.LastName}" : "Not Assigned");

                    int totalTasks = project.Tasks.Count;
                    int completedTasks = 0;

                    foreach (var task in project.Tasks)
                    {
                        if (task.Status == "Completed")
                        {
                            completedTasks++;
                        }
                    }

                    table.AddCell(totalTasks.ToString());
                    table.AddCell(completedTasks.ToString());
                }

                document.Add(table);
                document.Close();

                return File(ms.ToArray(), "application/pdf", "ProjectSummary.pdf");
            }
        }

        public IActionResult ExportEmployeePerformancePdf()
        {
            var employees = _context.Users.Include(u => u.AssignedTasks).Include(u => u.Department).ToList();

            using (MemoryStream ms = new MemoryStream())
            {
                PdfWriter writer = new PdfWriter(ms);
                PdfDocument pdf = new PdfDocument(writer);
                Document document = new Document(pdf);

                // Add header
                Paragraph header = new Paragraph("Employee Performance Report")
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontSize(20);
                document.Add(header);
                document.Add(new Paragraph("\n"));

                // Create table
                Table table = new Table(5).UseAllAvailableWidth();

                // Add table headers
                table.AddHeaderCell("Employee Name");
                table.AddHeaderCell("Department");
                table.AddHeaderCell("Total Tasks");
                table.AddHeaderCell("Completed Tasks");
                table.AddHeaderCell("Performance");

                // Add table data
                foreach (var employee in employees)
                {
                    table.AddCell($"{employee.FirstName} {employee.LastName}");
                    table.AddCell(employee.Department != null ? employee.Department.DepartmentName : "Not Assigned");

                    int totalTasks = employee.AssignedTasks.Count;
                    int completedTasks = 0;

                    foreach (var task in employee.AssignedTasks)
                    {
                        if (task.Status == "Completed")
                        {
                            completedTasks++;
                        }
                    }

                    int performance = totalTasks > 0 ? (completedTasks * 100 / totalTasks) : 0;

                    table.AddCell(totalTasks.ToString());
                    table.AddCell(completedTasks.ToString());
                    table.AddCell($"{performance}%");
                }

                document.Add(table);
                document.Close();

                return File(ms.ToArray(), "application/pdf", "EmployeePerformance.pdf");
            }
        }
    }
}