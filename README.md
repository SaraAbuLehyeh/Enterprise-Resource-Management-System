# Enterprise Resource Management System (ERMS) - Capstone Project

## Project Overview

The Enterprise Resource Management System (ERMS) is a comprehensive web-based platform designed and developed as a Capstone project by Sara Abdulheyh. It provides a centralized solution for managing essential organizational resources: employees, projects, and associated tasks.

This application demonstrates proficiency in full-stack development using the ASP.NET Core ecosystem, featuring:
*   **Role-Based Access Control:** Secure authentication and authorization using ASP.NET Core Identity with distinct roles (Admin, Manager, Employee).
*   **Modular Management:** CRUD operations for Employees, Departments, Projects, and Tasks through user-friendly interfaces.
*   **Database Integration:** Robust data persistence using SQL Server and Entity Framework Core Code-First migrations.
*   **RESTful API:** A secure, JWT-authenticated Web API for programmatic access to core resources.
*   **Hybrid Architecture:** Combines server-rendered MVC patterns for core views and forms with dynamic client-side interactions powered by JavaScript consuming the secure Web API.
*   **Security:** Implements standard security practices including CSRF protection, XSS mitigation, parameterized queries, and secure API authentication.
*   **Logging:** Utilizes NLog for configurable application logging.
*   **[Optional] Reporting:** Includes reports generated via [SSRS / Report Builder / Mention if not implemented].
*   **[Optional] Advanced Data Operations:** Utilizes Stored Procedures for [Describe purpose, e.g., data aggregation, specific updates]. [Update/Remove if not implemented].

## Technologies Used

*   **Backend:** ASP.NET Core 8 (MVC & Web API) [Update version if different]
*   **Database:** Microsoft SQL Server [Specify version if known, e.g., 2019 / Azure SQL]
*   **ORM:** Entity Framework Core 8 [Update version]
*   **Authentication:**
    *   ASP.NET Core Identity (Cookie-based for MVC)
    *   JWT Bearer Tokens (for Web API)
*   **Frontend:**
    *   Razor Views (.cshtml)
    *   HTML5, CSS3
    *   Bootstrap 5 [Update version]
    *   JavaScript (ES6+, Fetch API)
*   **Logging:** NLog.Web.AspNetCore [Update version if needed]
*   **Testing:** xUnit, Moq, Moq.EntityFrameworkCore [Update versions]
*   **IDE:** Visual Studio 2022 [Update version]
*   **Version Control:** Git, [GitHub / GitLab - Specify]
*   **[Optional] Reporting:** [SSRS / Report Builder / Power BI - Specify tool]
*   **[Optional] Deployment:** [Azure App Service / IIS / Other - Specify platform]

## Architecture & Implementation Strategy

ERMS is built as a single ASP.NET Core application employing a hybrid approach:

1.  **MVC Layer:** Handles user interface rendering, navigation, standard form submissions (POST requests for Create/Edit/Delete actions), and initial page data loading. It interacts directly with the `ApplicationDbContext` using Entity Framework Core. Authentication for MVC actions is managed by ASP.NET Core Identity's **cookie** system (`[Authorize]`, `[Authorize(Roles=...)]`), and forms are protected against CSRF using `[ValidateAntiForgeryToken]`.

2.  **Web API Layer:** Provides a separate set of RESTful endpoints under the `/api/...` route prefix. These endpoints expose CRUD operations for Employees (Users), Projects, and Tasks, primarily intended for programmatic or dynamic client-side access. The API is secured independently using **JWT Bearer token** authentication (`[Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme)]`).

3.  **API Consumption Strategy:** To enable dynamic UI updates and actions without full page reloads, **client-side JavaScript** running within the MVC views consumes the secured Web API.
    *   **Authentication Bridge:** When JavaScript needs to call a secured API endpoint, it first checks `sessionStorage` for a valid JWT.
    *   If no valid token exists, it makes a `GET` request to the dedicated, **cookie-protected** API endpoint `/api/Auth/get-my-token`. Because this request is initiated by the browser for a user already logged in via an Identity cookie, the server can validate the cookie, identify the user, generate a short-lived JWT, and return it.
    *   The JavaScript stores this received JWT in `sessionStorage`.
    *   For subsequent calls to data APIs (e.g., `GET /api/TasksApi?projectId=...`), the JavaScript retrieves the JWT from `sessionStorage` and includes it in the `Authorization: Bearer <token>` header of the `fetch` request.
    *   This hybrid approach keeps MVC authentication standard (cookies) while enabling secure, stateless API consumption from the client-side using JWTs.

4.  **Data Access:** Entity Framework Core (Code-First) manages the SQL Server database schema and interactions. Standard LINQ queries and EF Core methods provide protection against SQL injection. [Mention if Stored Procedures are used, e.g., "Stored procedures like `spGetProjectTaskSummary` are called using `FromSqlRaw` for specific reporting queries."]

5.  **Logging:** NLog is configured via `nlog.config` to provide file and console logging for diagnostics and monitoring.

6.  **Error Handling:** A global middleware (`GlobalExceptionHandlerMiddleware`) intercepts unhandled exceptions in the API pipeline and returns standardized `ProblemDetails` JSON responses. Standard MVC exception handling (`UseExceptionHandler`) is configured for view-related errors in production.

## Features

*   **Authentication:** Login, Registration [Update if limited], Logout.
*   **Role Management:** Admin users can manage user roles [Describe specific functionality if implemented, e.g., via AdminController].
*   **User Locking:** Admin users can lock/unlock user accounts.
*   **Department Management:** CRUD operations for Departments (Admin).
*   **Project Management:** CRUD operations for Projects (Admin/Manager), assign managers.
*   **Task Management:** CRUD operations for Tasks (Admin/Manager), assign tasks, set priority/status.
*   **Personal Task View:** Logged-in users can view tasks assigned specifically to them (`/Task/MyTasks`).
*   **Dynamic Task Loading:** Project Details page dynamically loads associated tasks using JavaScript and the API.
*   **Reporting:** [Describe the available reports, e.g., Project Summary Report via SSRS]. [Update/Remove if not applicable]
*   **Admin Utilities:** [Describe admin functions, e.g., Mark Overdue Tasks via SP]. [Update/Remove if not applicable]

## Getting Started

### Prerequisites

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) [Adjust version]
*   [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) (e.g., Developer Edition, Express Edition) or Azure SQL Database.
*   [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/) (Community Edition or higher) with ASP.NET and web development workload, and SQL Server Data Tools (optional, for SSRS).
*   [Git](https://git-scm.com/downloads/)

### Setup and Configuration

1.  **Clone the repository:**
    ```bash
    git clone [Your Repository URL]
    cd [Your Repository Folder]
    ```
2.  **Configure Database Connection:**
    *   Open `appsettings.json` (or more securely, manage user secrets for development).
    *   Locate the `ConnectionStrings` section.
    *   Update the `DefaultConnection` string to point to your SQL Server instance and choose a database name (e.g., `ERMS_Db`).
        ```json
          "ConnectionStrings": {
            "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=ERMS_Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
          },
        ```
3.  **Configure JWT Settings (Development):**
    *   Open `appsettings.json`.
    *   Review the `Jwt` section. For development, ensure `Key`, `Issuer`, and `Audience` are set. The Issuer/Audience should generally match the HTTPS URL your application runs on (e.g., from `launchSettings.json`).
        ```json
         "Jwt": {
            "Key": "[A_LONG_RANDOM_SECRET_KEY_FOR_DEVELOPMENT_ONLY!]",
            "Issuer": "https://localhost:7171", // Match your launch URL
            "Audience": "https://localhost:7171" // Match your launch URL
         },
        ```
    *   **Note:** For Production, the JWT Key must be strong and stored securely outside configuration files (e.g., Azure Key Vault, Environment Variables).
4.  **Apply Database Migrations:**
    *   Open the Package Manager Console in Visual Studio (Tools -> NuGet Package Manager -> Package Manager Console).
    *   Make sure the `ERMS` project is selected as the Default project.
    *   Run the command: `Update-Database`
    *   This will create the database (if it doesn't exist) and apply all EF Core migrations to set up the tables and seed initial data (like Roles).
5.  **Trust Development Certificate:**
    *   If running locally over HTTPS for the first time, you might need to trust the ASP.NET Core development certificate. Run this in a command prompt:
        ```bash
        dotnet dev-certs https --trust
        ```

### Running the Application

1.  Open the solution (`.sln` file) in Visual Studio 2022.
2.  Select `ERMS` as the startup project.
3.  Press `F5` or click the "Start Debugging" button (green play icon).
4.  The application should launch in your default web browser.
5.  [Optional: Describe how to register the first Admin user if not seeded, or provide default Admin credentials if seeded].

## Unit Testing

Unit tests are located in the `ERMS.Tests` project and utilize xUnit and Moq. To run tests:
*   Use the Test Explorer in Visual Studio.
*   Or run `dotnet test` in the solution directory from the command line.
*   Current Code Coverage: Approximately [Your Current Percentage]%. [Add brief note as per Proposal].

## [Optional] Reporting

Reports [List reports] are available via [Describe how to access - SSRS Portal URL, link within app?]. Requires setup of [SSRS / Report Builder] and configuration of the report server connection.

## [Optional] Deployment

[Add notes on deploying the application, focusing on configuring the production environment, database connection strings, and secure JWT secret management using Azure Key Vault, Environment Variables, or similar.]