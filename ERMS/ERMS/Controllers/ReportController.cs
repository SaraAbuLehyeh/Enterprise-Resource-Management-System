// File: Controllers/ReportController.cs (Example)
using ERMS.Data;
using ERMS.Models; // For result model
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For FromSqlRaw, ToListAsync
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

[Authorize] // Secure reports
public class ReportController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReportController> _logger;

    public ReportController(ApplicationDbContext context, ILogger<ReportController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Action method to display the summary report
    public async Task<IActionResult> ProjectSummary()
    {
        _logger.LogInformation("Generating Project Summary Report using Stored Procedure.");
        try
        {
            // Execute the SP and map results to the model
            var summaryData = await _context.Set<ProjectTaskSummaryResult>()
                                          .FromSqlRaw("EXEC dbo.spGetProjectTaskSummary")
                                          .ToListAsync();

            return View(summaryData); // Pass data to a view named ProjectSummary.cshtml
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing spGetProjectTaskSummary.");
            // Handle error - perhaps show an error view or message
            TempData["ErrorMessage"] = "Error generating project summary report.";
            return RedirectToAction("Index", "Home"); // Or an error page
        }
    }

    // Other report actions...
}