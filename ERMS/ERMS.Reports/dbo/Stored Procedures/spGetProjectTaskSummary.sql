
-- Create the stored procedure
CREATE PROCEDURE dbo.spGetProjectTaskSummary
AS
BEGIN
    -- Prevent extra messages interfering with results
    SET NOCOUNT ON;

    -- Select Project details along with task counts
    SELECT
        P.ProjectID,
        P.ProjectName,
        P.StartDate,
        P.EndDate,
        U.FirstName + ' ' + U.LastName AS ManagerName,
        ISNULL(TaskCounts.TotalTasks, 0) AS TotalTasks,       -- Total tasks for the project
        ISNULL(TaskCounts.CompletedTasks, 0) AS CompletedTasks  -- Completed tasks for the project
    FROM
        dbo.Projects AS P
    INNER JOIN
        dbo.AspNetUsers AS U ON P.ManagerID = U.Id -- Join to get Manager Name
    LEFT JOIN
        (
            -- Subquery to count total and completed tasks per project
            SELECT
                ProjectID,
                COUNT(TaskID) AS TotalTasks,
                SUM(CASE WHEN Status = 'Completed' THEN 1 ELSE 0 END) AS CompletedTasks
            FROM
                dbo.Tasks -- Use your actual Tasks table name if different
            GROUP BY
                ProjectID
        ) AS TaskCounts ON P.ProjectID = TaskCounts.ProjectID
    ORDER BY
        P.ProjectName;

END
