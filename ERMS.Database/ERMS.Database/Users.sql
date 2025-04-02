CREATE TABLE [dbo].[Users]
(
    [UserID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FirstName] NVARCHAR(50) NOT NULL,
    [LastName] NVARCHAR(50) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [PasswordHash] NVARCHAR(128) NOT NULL,
    [Role] NVARCHAR(20) NOT NULL,
    [HireDate] DATETIME NOT NULL,
    [DepartmentID] INT NOT NULL,
    CONSTRAINT [FK_Users_Departments] FOREIGN KEY ([DepartmentID]) 
        REFERENCES [dbo].[Departments]([DepartmentID])
)
