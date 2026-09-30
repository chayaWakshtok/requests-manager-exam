-- Same definitions as the EF Core migration (ServiceRequestConfiguration).
CREATE INDEX IX_Requests_Status_CreatedAt ON Requests (Status, CreatedAt) INCLUDE (Priority);
CREATE INDEX IX_Requests_CreatedAt ON Requests (CreatedAt);
CREATE INDEX IX_Requests_OrganizationName ON Requests (OrganizationName) INCLUDE (Status);
CREATE INDEX IX_Requests_AssignedTo_Status ON Requests (AssignedTo, Status);
CREATE INDEX IX_Requests_SearchText ON Requests (SearchText);
