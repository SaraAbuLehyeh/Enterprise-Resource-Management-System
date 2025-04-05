CREATE TABLE [dbo].[Departments] (
    [DepartmentID]   INT           IDENTITY (1, 1) NOT NULL,
    [DepartmentName] NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Departments] PRIMARY KEY CLUSTERED ([DepartmentID] ASC)
);

