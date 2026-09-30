-- Performance benchmark for the main API queries on the seeded dataset (100,000 requests).
-- The statements are the exact SQL EF Core generates (captured from the API log), run through
-- sp_executesql with the same parameters so the plans match what the application gets.
-- Run: docker exec -i <sql-container> /opt/mssql-tools18/bin/sqlcmd -C -I -S localhost -U sa -P '<pwd>' -d RequestsManager -i benchmark.sql
SET NOCOUNT ON;
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

PRINT '=== Q1: list - status IN (New, InProgress) AND priority = High, ORDER BY CreatedAt DESC, page 3 ===';
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r]
WHERE [r].[Status] IN (SELECT [q].[value] FROM OPENJSON(@s) WITH ([value] tinyint ''$'') AS [q])
  AND [r].[Priority] IN (SELECT [q0].[value] FROM OPENJSON(@p) WITH ([value] tinyint ''$'') AS [q0])',
  N'@s nvarchar(4000), @p nvarchar(4000)', @s = N'[0,1]', @p = N'[2]';
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
WHERE [r].[Status] IN (SELECT [q].[value] FROM OPENJSON(@s) WITH ([value] tinyint ''$'') AS [q])
  AND [r].[Priority] IN (SELECT [q0].[value] FROM OPENJSON(@p) WITH ([value] tinyint ''$'') AS [q0])
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC
OFFSET @o ROWS FETCH NEXT @n ROWS ONLY',
  N'@s nvarchar(4000), @p nvarchar(4000), @o int, @n int', @s = N'[0,1]', @p = N'[2]', @o = 40, @n = 20;

PRINT '=== Q2: list - default screen, no filter, ORDER BY CreatedAt DESC, page 1 ===';
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC OFFSET @o ROWS FETCH NEXT @n ROWS ONLY',
  N'@o int, @n int', @o = 0, @n = 20;

PRINT '=== Q3: list - organization prefix ''Acme%'' + created-at range, ORDER BY OrganizationName ===';
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r]
WHERE [r].[OrganizationName] LIKE @org ESCAPE N''\'' AND [r].[CreatedAt] >= @from AND [r].[CreatedAt] <= @to',
  N'@org nvarchar(200), @from datetime2(3), @to datetime2(3)', @org = N'Acme%', @from = '2026-01-01', @to = '2026-06-30';
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
WHERE [r].[OrganizationName] LIKE @org ESCAPE N''\'' AND [r].[CreatedAt] >= @from AND [r].[CreatedAt] <= @to
ORDER BY [r].[OrganizationName], [r].[Id] OFFSET @o ROWS FETCH NEXT @n ROWS ONLY',
  N'@org nvarchar(200), @from datetime2(3), @to datetime2(3), @o int, @n int', @org = N'Acme%', @from = '2026-01-01', @to = '2026-06-30', @o = 0, @n = 20;

PRINT '=== Q4: list - assignee + open statuses ===';
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r]
WHERE [r].[AssignedTo] = @a AND [r].[Status] IN (SELECT [q].[value] FROM OPENJSON(@s) WITH ([value] tinyint ''$'') AS [q])',
  N'@a nvarchar(100), @s nvarchar(4000)', @a = N'agent07', @s = N'[1,2]';

PRINT '=== Q5a: free-text search, naive version - Title LIKE ''%payroll%'' OR OrganizationName LIKE ... ===';
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @t ESCAPE N''\'' OR [r].[OrganizationName] LIKE @t ESCAPE N''\''',
  N'@t nvarchar(200)', @t = N'%payroll%';
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r]
WHERE [r].[Title] LIKE @t ESCAPE N''\'' OR [r].[OrganizationName] LIKE @t ESCAPE N''\''
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC OFFSET @o ROWS FETCH NEXT @n ROWS ONLY',
  N'@t nvarchar(200), @o int, @n int', @t = N'%payroll%', @o = 0, @n = 20;

PRINT '=== Q5b: free-text search, final version - SearchText (UPPER, BIN2 collation) LIKE ''%PAYROLL%'' ===';
EXEC sp_executesql N'SELECT COUNT(*) FROM [Requests] AS [r] WHERE [r].[SearchText] LIKE @t ESCAPE N''\''',
  N'@t nvarchar(401)', @t = N'%PAYROLL%';
EXEC sp_executesql N'SELECT [r].[Id], [r].[Title], [r].[OrganizationName], [r].[Status], [r].[Priority], [r].[AssignedTo], [r].[CreatedAt], [r].[UpdatedAt], [r].[RowVersion]
FROM [Requests] AS [r] WHERE [r].[SearchText] LIKE @t ESCAPE N''\''
ORDER BY [r].[CreatedAt] DESC, [r].[Id] DESC OFFSET @o ROWS FETCH NEXT @n ROWS ONLY',
  N'@t nvarchar(401), @o int, @n int', @t = N'%PAYROLL%', @o = 0, @n = 20;

PRINT '=== Q6: stats - count by status ===';
SELECT [r].[Status], COUNT(*) FROM [Requests] AS [r] GROUP BY [r].[Status];

PRINT '=== Q7: stats - open by priority ===';
SELECT [r].[Priority], COUNT(*) FROM [Requests] AS [r] WHERE [r].[Status] <> CAST(3 AS tinyint) GROUP BY [r].[Priority];

PRINT '=== Q8: stats - top 5 organizations by open requests ===';
EXEC sp_executesql N'SELECT TOP(@n) [r].[OrganizationName], COUNT(*) FROM [Requests] AS [r]
WHERE [r].[Status] <> CAST(3 AS tinyint) GROUP BY [r].[OrganizationName] ORDER BY COUNT(*) DESC, [r].[OrganizationName]',
  N'@n int', @n = 5;

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;
