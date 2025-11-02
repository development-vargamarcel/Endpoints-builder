-- ============================================================================
-- Endpoint Library - Test Database Setup
-- Table: testepoint
-- ============================================================================
-- NOTE: Run DROP TABLE IF EXISTS testepoint manually before running this script
-- ============================================================================

-- Create the testepoint table with various field types for comprehensive testing
CREATE TABLE testepoint (
    -- Primary Key
    EndpointId INT IDENTITY(1,1) PRIMARY KEY,

    -- Basic Information Fields
    EndpointName NVARCHAR(100) NOT NULL,
    EndpointType NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500),

    -- Status and Control Fields
    IsActive BIT DEFAULT 1,
    Status NVARCHAR(20) DEFAULT 'ACTIVE',
    Priority INT DEFAULT 5,

    -- User/Ownership Fields
    CreatedBy NVARCHAR(100) NOT NULL,
    OwnerId NVARCHAR(50),

    -- Date/Time Fields
    CreatedDate DATETIME DEFAULT GETDATE(),
    LastModifiedDate DATETIME,
    LastAccessDate DATETIME,
    ExpiryDate DATE,

    -- Numeric Fields (for testing various numeric operations)
    RequestCount INT DEFAULT 0,
    ErrorCount INT DEFAULT 0,
    AvgResponseTime DECIMAL(10,2),
    MaxPayloadSize BIGINT,

    -- Configuration/JSON Fields
    ConfigJson NVARCHAR(MAX),
    Metadata NVARCHAR(1000),

    -- Special Characters Testing
    SpecialChars NVARCHAR(200),

    -- Sensitive Data (for field exclusion testing)
    ApiKey NVARCHAR(100),
    SecretToken NVARCHAR(200),

    -- Version Control
    Version INT DEFAULT 1,

    -- Soft Delete
    IsDeleted BIT DEFAULT 0,
    DeletedDate DATETIME,
    DeletedBy NVARCHAR(100)
);

-- Create indexes for better performance
CREATE INDEX IX_testepoint_Status ON testepoint(Status) WHERE IsDeleted = 0;
CREATE INDEX IX_testepoint_Type ON testepoint(EndpointType) WHERE IsDeleted = 0;
CREATE INDEX IX_testepoint_CreatedDate ON testepoint(CreatedDate);
CREATE INDEX IX_testepoint_OwnerId ON testepoint(OwnerId) WHERE IsDeleted = 0;

-- ============================================================================
-- Test Data - Comprehensive scenarios
-- ============================================================================

-- 1. Active endpoints with various types
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, CreatedDate, RequestCount, ErrorCount, AvgResponseTime, MaxPayloadSize, ConfigJson, Metadata, ApiKey, SecretToken)
VALUES
    ('UserManagement', 'CRUD', 'User management endpoint with full CRUD operations', 1, 'ACTIVE', 10, 'admin', 'USR001', '2025-01-15 10:00:00', 1500, 5, 125.50, 1048576, '{"timeout":30,"retries":3}', 'version:1.0,region:us-east', 'API_KEY_USER_MGT_001', 'SECRET_TOKEN_XYZ123'),

    ('OrderProcessing', 'CRUD', 'Order processing and fulfillment endpoint', 1, 'ACTIVE', 9, 'admin', 'USR001', '2025-01-16 11:30:00', 2300, 12, 210.75, 2097152, '{"timeout":60,"retries":5}', 'version:1.1,region:us-west', 'API_KEY_ORDER_PROC_002', 'SECRET_TOKEN_ABC456'),

    ('ProductCatalog', 'READ', 'Read-only product catalog endpoint', 1, 'ACTIVE', 8, 'devuser1', 'USR002', '2025-01-17 09:15:00', 5600, 3, 85.25, 524288, '{"cache":true,"ttl":300}', 'version:2.0,region:eu-central', 'API_KEY_PROD_CAT_003', 'SECRET_TOKEN_DEF789'),

    ('DataExport', 'BATCH', 'Batch data export endpoint for reporting', 1, 'ACTIVE', 7, 'devuser2', 'USR003', '2025-01-18 14:20:00', 450, 8, 3500.00, 10485760, '{"batchSize":1000,"parallel":true}', 'version:1.5,region:ap-southeast', 'API_KEY_DATA_EXP_004', 'SECRET_TOKEN_GHI012'),

    ('NotificationService', 'WRITE', 'Push notification service endpoint', 1, 'ACTIVE', 10, 'admin', 'USR001', '2025-01-19 08:45:00', 8900, 45, 95.80, 102400, '{"provider":"firebase","priority":"high"}', 'version:1.0,region:global', 'API_KEY_NOTIF_SVC_005', 'SECRET_TOKEN_JKL345');

-- 2. Inactive/Disabled endpoints
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, CreatedDate, LastModifiedDate, RequestCount, ErrorCount, AvgResponseTime)
VALUES
    ('LegacyReporting', 'READ', 'Legacy reporting endpoint - deprecated', 0, 'INACTIVE', 1, 'admin', 'USR001', '2024-06-10 12:00:00', '2024-12-15 16:30:00', 15000, 230, 1850.50),

    ('OldAuthService', 'WRITE', 'Old authentication service - replaced', 0, 'DISABLED', 1, 'sysadmin', 'USR004', '2024-03-20 10:00:00', '2024-11-01 14:00:00', 45000, 1200, 450.00);

-- 3. Endpoints in maintenance/testing status
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, CreatedDate, RequestCount, ErrorCount, AvgResponseTime, ExpiryDate)
VALUES
    ('TestEndpoint_Alpha', 'CRUD', 'Testing endpoint for alpha features', 1, 'TESTING', 5, 'devuser3', 'USR002', '2025-01-25 10:00:00', 50, 2, 150.00, '2025-02-28'),

    ('MaintenanceAPI', 'WRITE', 'Undergoing maintenance and upgrades', 0, 'MAINTENANCE', 6, 'devuser1', 'USR002', '2025-01-20 15:00:00', 3400, 15, 220.00, '2025-02-15'),

    ('BetaFeature', 'BATCH', 'Beta feature endpoint - limited access', 1, 'BETA', 4, 'devuser2', 'USR003', '2025-01-28 11:00:00', 120, 5, 380.00, '2025-03-31');

-- 4. Endpoints with special characters (for testing SQL injection prevention and encoding)
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, CreatedDate, SpecialChars, RequestCount, ErrorCount, AvgResponseTime)
VALUES
    ('SpecialCharsTest', 'READ', 'Test endpoint with special characters', 1, 'ACTIVE', 5, 'testuser', 'USR005', '2025-01-29 12:00:00', 'Test with ''quotes'' and "double" & <tags> % wildcards', 25, 0, 100.00),

    ('SQLInjectionTest', 'READ', 'Security testing endpoint', 1, 'ACTIVE', 5, 'secuser', 'USR006', '2025-01-30 13:00:00', 'OR 1=1; DROP TABLE--', 10, 0, 95.00),

    ('UnicodeTest', 'READ', 'Unicode and international characters test', 1, 'ACTIVE', 5, 'testuser', 'USR005', '2025-01-31 14:00:00', 'Tëst Dàtà 测试 テスト مرحبا', 15, 0, 105.00);

-- 5. Endpoints with NULL values (for testing NULL handling)
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, LastModifiedDate, LastAccessDate, ExpiryDate, AvgResponseTime, MaxPayloadSize, ConfigJson, Metadata, SpecialChars)
VALUES
    ('MinimalEndpoint', 'READ', 'Minimal configuration endpoint', 1, 'ACTIVE', 5, 'testuser', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),

    ('PartialData', 'WRITE', NULL, 1, 'ACTIVE', 5, 'testuser', 'USR007', NULL, NULL, NULL, NULL, NULL, NULL, 'partial:true', NULL);

-- 6. Soft-deleted endpoints (for testing soft delete functionality)
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, CreatedDate, IsDeleted, DeletedDate, DeletedBy, RequestCount, ErrorCount)
VALUES
    ('DeletedEndpoint1', 'CRUD', 'This endpoint was soft deleted', 0, 'DELETED', 1, 'admin', 'USR001', '2024-08-15 10:00:00', 1, '2025-01-10 16:30:00', 'admin', 5000, 150),

    ('DeletedEndpoint2', 'WRITE', 'Another soft deleted endpoint', 0, 'DELETED', 1, 'devuser1', 'USR002', '2024-09-20 12:00:00', 1, '2025-01-12 09:15:00', 'sysadmin', 2300, 45);

-- 7. High-volume endpoints (for performance testing)
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, CreatedDate, RequestCount, ErrorCount, AvgResponseTime, MaxPayloadSize)
VALUES
    ('HighVolumeAPI1', 'READ', 'High traffic read endpoint', 1, 'ACTIVE', 10, 'admin', 'USR001', '2024-12-01 08:00:00', 1500000, 350, 45.20, 10240),

    ('HighVolumeAPI2', 'WRITE', 'High traffic write endpoint', 1, 'ACTIVE', 10, 'admin', 'USR001', '2024-12-05 08:00:00', 850000, 1250, 180.50, 51200),

    ('BulkProcessor', 'BATCH', 'Bulk processing endpoint - large payloads', 1, 'ACTIVE', 9, 'sysadmin', 'USR004', '2024-12-10 10:00:00', 125000, 890, 5200.75, 104857600);

-- 8. Endpoints with various priorities (for testing ORDER BY)
INSERT INTO testepoint (EndpointName, EndpointType, Description, IsActive, Status, Priority, CreatedBy, OwnerId, CreatedDate, RequestCount)
VALUES
    ('Critical_P10', 'CRUD', 'Critical priority endpoint', 1, 'ACTIVE', 10, 'admin', 'USR001', '2025-02-01 10:00:00', 1000),
    ('High_P8', 'READ', 'High priority endpoint', 1, 'ACTIVE', 8, 'devuser1', 'USR002', '2025-02-01 10:05:00', 800),
    ('Medium_P5', 'WRITE', 'Medium priority endpoint', 1, 'ACTIVE', 5, 'devuser2', 'USR003', '2025-02-01 10:10:00', 500),
    ('Low_P3', 'BATCH', 'Low priority endpoint', 1, 'ACTIVE', 3, 'testuser', 'USR005', '2025-02-01 10:15:00', 300),
    ('VeryLow_P1', 'READ', 'Very low priority endpoint', 1, 'ACTIVE', 1, 'testuser', 'USR005', '2025-02-01 10:20:00', 100);

-- ============================================================================
-- Data Verification
-- ============================================================================

-- Count of test records
SELECT
    Status,
    EndpointType,
    COUNT(*) as RecordCount
FROM testepoint
GROUP BY Status, EndpointType
ORDER BY Status, EndpointType;

-- Summary statistics
SELECT
    COUNT(*) as TotalRecords,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) as ActiveCount,
    SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) as DeletedCount,
    COUNT(DISTINCT EndpointType) as UniqueTypes,
    COUNT(DISTINCT OwnerId) as UniqueOwners,
    AVG(RequestCount) as AvgRequests,
    AVG(ErrorCount) as AvgErrors,
    AVG(AvgResponseTime) as OverallAvgResponseTime
FROM testepoint;

PRINT 'Test data setup completed successfully!';
