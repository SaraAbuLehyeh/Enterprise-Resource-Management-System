CREATE TABLE [dbo].[Tasks] (
    [TaskID]      INT            IDENTITY (1, 1) NOT NULL,
    [ProjectID]   INT            NOT NULL,
    [AssigneeID]  NVARCHAR (450) NOT NULL,
    [TaskName]    NVARCHAR (100) NOT NULL,
    [Description] NVARCHAR (MAX) NOT NULL,
    [DueDate]     DATETIME2 (7)  NOT NULL,
    [Priority]    NVARCHAR (50)  NOT NULL,
    [Status]      NVARCHAR (50)  NOT NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY CLUSTERED ([TaskID] ASC),
    CONSTRAINT [FK_Tasks_AspNetUsers_AssigneeID] FOREIGN KEY ([AssigneeID]) REFERENCES [dbo].[AspNetUsers] ([Id]),
    CONSTRAINT [FK_Tasks_Projects_ProjectID] FOREIGN KEY ([ProjectID]) REFERENCES [dbo].[Projects] ([ProjectID]) ON DELETE CASCADE
);


GO
CREATE NONCLUSTERED INDEX [IX_Tasks_AssigneeID]
    ON [dbo].[Tasks]([AssigneeID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Tasks_DueDate]
    ON [dbo].[Tasks]([DueDate] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Tasks_ProjectID]
    ON [dbo].[Tasks]([ProjectID] ASC);

