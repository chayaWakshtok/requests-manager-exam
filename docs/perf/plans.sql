-- Actual execution plans (STATISTICS PROFILE) for the main operations; see performance.md.
-- Run with: sqlcmd -C -I -W -s "|" ... -i plans.sql
SET NOCOUNT ON;
SET STATISTICS PROFILE ON;
PRINT '=== P1: list, default screen (ORDER BY CreatedAt DESC, Id DESC, page 1) ===';
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC OFFSET @o ROWS FETCH NEXT @n ROWS ONLY', N'@o int, @n int', @o = 0, @n = 20;
PRINT '=== P2: list, organization prefix + date range ===';
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
WHERE [r].[OrganizationName] LIKE @org ESCAPE N''\'' AND [r].[CreatedAt] >= @from AND [r].[CreatedAt] <= @to
ORDER BY [r].[OrganizationName], [r].[Id] OFFSET @o ROWS FETCH NEXT @n ROWS ONLY',
  N'@org nvarchar(200), @from datetime2(3), @to datetime2(3), @o int, @n int', @org = N'Acme%', @from = '2026-01-01', @to = '2026-06-30', @o = 0, @n = 20;
PRINT '=== P3: free-text search count ===';
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r] WHERE [r].[SearchText] LIKE @t ESCAPE N''\''', N'@t nvarchar(401)', @t = N'%PAYROLL%';
PRINT '=== P4: stats, open by priority ===';
SELECT [r].[Priority], COUNT(*) FROM [Requests] AS [r] WHERE [r].[Status] <> CAST(3 AS tinyint) GROUP BY [r].[Priority];
PRINT '=== P5: stats, top organizations ===';
EXEC sp_executesql N'SELECT TOP(@n) [r].[OrganizationName], COUNT(*) FROM [Requests] AS [r]
WHERE [r].[Status] <> CAST(3 AS tinyint) GROUP BY [r].[OrganizationName] ORDER BY COUNT(*) DESC, [r].[OrganizationName]', N'@n int', @n = 5;
SET STATISTICS PROFILE OFF;
