
-- Create the stored procedure
CREATE PROCEDURE dbo.spUpdateTaskStatusBulk
    @TargetStatus NVARCHAR(50), -- The status to update *to* (e.g., 'Overdue')
    @CurrentStatus NVARCHAR(50), -- The status to find (e.g., 'Not Started')
    @DueDateThreshold DATETIME2 -- Tasks due on or before this date (e.g., GETDATE())
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @RowsAffected INT = 0;

    BEGIN TRY
        UPDATE dbo.Tasks -- Use your actual Tasks table name
        SET
            Status = @TargetStatus
        WHERE
            Status = @CurrentStatus
            AND DueDate <= @DueDateThreshold;

        SET @RowsAffected = @@ROWCOUNT; -- Get the number of rows updated

        -- Optionally, return the number of affected rows
        SELECT @RowsAffected AS NumberOfTasksUpdated;

    END TRY
    BEGIN CATCH
        -- Basic error handling (consider more advanced logging/re-throwing)
        PRINT 'Error occurred during bulk task update.';
        PRINT ERROR_MESSAGE();
         -- Return -1 or throw error to indicate failure
         SELECT -1 AS NumberOfTasksUpdated; -- Indicate failure
         -- THROW; -- Optionally re-throw the error
    END CATCH
END
