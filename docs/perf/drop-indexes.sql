-- Baseline for the benchmark: remove the nonclustered indexes of Requests (only the PK stays).
DROP INDEX IF EXISTS IX_Requests_Status_CreatedAt ON Requests;
DROP INDEX IF EXISTS IX_Requests_CreatedAt ON Requests;
DROP INDEX IF EXISTS IX_Requests_OrganizationName ON Requests;
DROP INDEX IF EXISTS IX_Requests_AssignedTo_Status ON Requests;
DROP INDEX IF EXISTS IX_Requests_SearchText ON Requests;
