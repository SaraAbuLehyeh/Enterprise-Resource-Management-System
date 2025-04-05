using System.Net; // Required for HttpStatusCode
using System.Text.Json; // Required for JsonSerializer
using Microsoft.AspNetCore.Mvc; // Required for ProblemDetails

namespace ERMS.Middleware // Adjust namespace if needed
{
    /// <summary>
    /// Middleware to handle exceptions globally, log them, and return standardized ProblemDetails responses.
    /// </summary>
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _env; // To check if running in Development

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalExceptionHandlerMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="env">The hosting environment information.</param>
        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment env)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        /// <summary>
        /// Invokes the middleware to handle the request.
        /// </summary>
        /// <param name="context">The HTTP context.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Call the next middleware in the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Log the exception with details
                _logger.LogError(ex, "An unhandled exception occurred processing request: {Path}", context.Request.Path);

                // Create a ProblemDetails response
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode statusCode;
            string title;
            string detail;

            // Customize response based on exception type (add more specific exceptions as needed)
            switch (exception)
            {
                // Example: Add custom exception types if you have them
                // case YourNotFoundException notFoundEx:
                //     statusCode = HttpStatusCode.NotFound;
                //     title = "Resource Not Found";
                //     detail = notFoundEx.Message;
                //     break;

                // case YourValidationException validationEx:
                //     statusCode = HttpStatusCode.BadRequest;
                //     title = "Validation Error";
                //     detail = validationEx.Message; // Or format validation errors
                //     break;

                // Default for unhandled exceptions
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    title = "An unexpected error occurred.";
                    // Only include detailed exception messages in Development environment for security
                    detail = _env.IsDevelopment() ? exception.ToString() : "An internal server error prevented processing the request.";
                    break;
            }

            // Create the ProblemDetails object
            var problemDetails = new ProblemDetails
            {
                Status = (int)statusCode,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path // Identifies the specific request instance where the error occurred
            };

            // Set response details
            context.Response.ContentType = "application/problem+json"; // Standard content type for ProblemDetails
            context.Response.StatusCode = (int)statusCode;

            // Serialize and write the response
            var jsonResponse = JsonSerializer.Serialize(problemDetails);
            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// Extension method to register the GlobalExceptionHandlerMiddleware.
    /// </summary>
    public static class GlobalExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        }
    }
}