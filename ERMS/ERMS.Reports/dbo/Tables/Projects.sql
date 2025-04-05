CREATE TABLE [dbo].[Projects] (
    [ProjectID]   INT            IDENTITY (1, 1) NOT NULL,
    [ProjectName] NVARCHAR (100) NOT NULL,
    [Description] NVARCHAR (MAX) NOT NULL,
    [StartDate]   DATETIME2 (7)  NOT NULL,
    [EndDate]     DATETIME2 (7)  NULL,
    [ManagerID]   NVARCHAR (450) NOT NULL,
    CONSTRAINT [PK_Projects] PRIMARY KEY CLUSTERED ([ProjectID] ASC),
    CONSTRAINT [FK_Projects_AspNetUsers_ManagerID] FOREIGN KEY ([ManagerID]) REFERENCES [dbo].[AspNetUsers] ([Id])
);


GO
CREATE NONCLUSTERED INDEX [IX_Projects_ManagerID]
    ON [dbo].[Projects]([ManagerID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_Projects_StartDate]
    ON [dbo].[Projects]([StartDate] ASC);

