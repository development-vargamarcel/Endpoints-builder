-- ============================================================================
-- Test Validation Script for testepoint
-- Quick validation queries to verify test data integrity and functionality
-- ============================================================================

PRINT '========================================';
PRINT 'TEST VALIDATION SCRIPT';
PRINT '========================================';
PRINT '';

-- ============================================================================
-- 1. TABLE STRUCTURE VALIDATION
-- ============================================================================
PRINT '1. TABLE STRUCTURE VALIDATION';
PRINT '------------------------------';

-- Check if table exists
IF OBJECT_ID('testepoint', 'U') IS NOT NULL
BEGIN
    PRINT '✓ Table testepoint exists';
END
ELSE
BEGIN
    PRINT '✗ ERROR: Table testepoint does not exist!';
    PRINT '   Please run testepoint_setup.sql first';
    RETURN;
END

-- Check column count
DECLARE @ColumnCount INT;
SELECT @ColumnCount = COUNT(*)
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'testepoint';

PRINT '  Column count: ' + CAST(@ColumnCount AS VARCHAR);
IF @ColumnCount >= 25
    PRINT '✓ All expected columns present';
ELSE
    PRINT '✗ WARNING: Expected at least 25 columns, found ' + CAST(@ColumnCount AS VARCHAR);

PRINT '';

-- ============================================================================
-- 2. DATA INTEGRITY VALIDATION
-- ============================================================================
PRINT '2. DATA INTEGRITY VALIDATION';
PRINT '-----------------------------';

-- Total record count
DECLARE @TotalRecords INT;
SELECT @TotalRecords = COUNT(*) FROM testepoint;
PRINT '  Total records: ' + CAST(@TotalRecords AS VARCHAR);

IF @TotalRecords >= 25
    PRINT '✓ Test data loaded successfully';
ELSE
    PRINT '✗ WARNING: Expected at least 25 test records, found ' + CAST(@TotalRecords AS VARCHAR);

-- Check for required data categories
DECLARE @ActiveCount INT, @InactiveCount INT, @DeletedCount INT;
DECLARE @CrudCount INT, @ReadCount INT, @WriteCount INT, @BatchCount INT;

SELECT @ActiveCount = COUNT(*) FROM testepoint WHERE IsActive = 1 AND IsDeleted = 0;
SELECT @InactiveCount = COUNT(*) FROM testepoint WHERE IsActive = 0 AND IsDeleted = 0;
SELECT @DeletedCount = COUNT(*) FROM testepoint WHERE IsDeleted = 1;

SELECT @CrudCount = COUNT(*) FROM testepoint WHERE EndpointType = 'CRUD' AND IsDeleted = 0;
SELECT @ReadCount = COUNT(*) FROM testepoint WHERE EndpointType = 'READ' AND IsDeleted = 0;
SELECT @WriteCount = COUNT(*) FROM testepoint WHERE EndpointType = 'WRITE' AND IsDeleted = 0;
SELECT @BatchCount = COUNT(*) FROM testepoint WHERE EndpointType = 'BATCH' AND IsDeleted = 0;

PRINT '';
PRINT '  Record Breakdown:';
PRINT '  - Active: ' + CAST(@ActiveCount AS VARCHAR);
PRINT '  - Inactive: ' + CAST(@InactiveCount AS VARCHAR);
PRINT '  - Deleted: ' + CAST(@DeletedCount AS VARCHAR);
PRINT '';
PRINT '  Type Breakdown:';
PRINT '  - CRUD: ' + CAST(@CrudCount AS VARCHAR);
PRINT '  - READ: ' + CAST(@ReadCount AS VARCHAR);
PRINT '  - WRITE: ' + CAST(@WriteCount AS VARCHAR);
PRINT '  - BATCH: ' + CAST(@BatchCount AS VARCHAR);

IF @ActiveCount > 0 AND @DeletedCount > 0
    PRINT '✓ Test data includes active and deleted records';
ELSE
    PRINT '✗ WARNING: Missing active or deleted test records';

IF @CrudCount > 0 AND @ReadCount > 0 AND @WriteCount > 0 AND @BatchCount > 0
    PRINT '✓ All endpoint types represented';
ELSE
    PRINT '✗ WARNING: Not all endpoint types present in test data';

PRINT '';

-- ============================================================================
-- 3. INDEX VALIDATION
-- ============================================================================
PRINT '3. INDEX VALIDATION';
PRINT '-------------------';

DECLARE @IndexCount INT;
SELECT @IndexCount = COUNT(*)
FROM sys.indexes
WHERE object_id = OBJECT_ID('testepoint')
  AND is_primary_key = 0;

PRINT '  Index count (excluding PK): ' + CAST(@IndexCount AS VARCHAR);

IF @IndexCount >= 4
    PRINT '✓ Expected indexes created';
ELSE
    PRINT '✗ WARNING: Expected at least 4 indexes, found ' + CAST(@IndexCount AS VARCHAR);

-- List indexes
SELECT
    i.name as IndexName,
    c.name as ColumnName,
    i.type_desc as IndexType
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('testepoint')
ORDER BY i.name, ic.key_ordinal;

PRINT '';

-- ============================================================================
-- 4. DATA QUALITY CHECKS
-- ============================================================================
PRINT '4. DATA QUALITY CHECKS';
PRINT '----------------------';

-- Check for NULL in required fields
DECLARE @NullEndpointNames INT, @NullEndpointTypes INT, @NullCreatedBy INT;

SELECT @NullEndpointNames = COUNT(*) FROM testepoint WHERE EndpointName IS NULL;
SELECT @NullEndpointTypes = COUNT(*) FROM testepoint WHERE EndpointType IS NULL;
SELECT @NullCreatedBy = COUNT(*) FROM testepoint WHERE CreatedBy IS NULL;

IF @NullEndpointNames = 0
    PRINT '✓ No NULL EndpointNames';
ELSE
    PRINT '✗ ERROR: Found ' + CAST(@NullEndpointNames AS VARCHAR) + ' NULL EndpointNames';

IF @NullEndpointTypes = 0
    PRINT '✓ No NULL EndpointTypes';
ELSE
    PRINT '✗ ERROR: Found ' + CAST(@NullEndpointTypes AS VARCHAR) + ' NULL EndpointTypes';

IF @NullCreatedBy = 0
    PRINT '✓ No NULL CreatedBy values';
ELSE
    PRINT '✗ ERROR: Found ' + CAST(@NullCreatedBy AS VARCHAR) + ' NULL CreatedBy values';

-- Check for duplicate endpoint names (should not exist for active records)
DECLARE @DuplicateNames INT;
SELECT @DuplicateNames = COUNT(*)
FROM (
    SELECT EndpointName, COUNT(*) as cnt
    FROM testepoint
    WHERE IsDeleted = 0
    GROUP BY EndpointName
    HAVING COUNT(*) > 1
) duplicates;

IF @DuplicateNames = 0
    PRINT '✓ No duplicate active endpoint names';
ELSE
    PRINT '✗ WARNING: Found ' + CAST(@DuplicateNames AS VARCHAR) + ' duplicate active endpoint names';

-- Check date logic
DECLARE @InvalidDates INT;
SELECT @InvalidDates = COUNT(*)
FROM testepoint
WHERE LastModifiedDate < CreatedDate;

IF @InvalidDates = 0
    PRINT '✓ Date logic valid (LastModifiedDate >= CreatedDate)';
ELSE
    PRINT '✗ WARNING: Found ' + CAST(@InvalidDates AS VARCHAR) + ' records with invalid date logic';

PRINT '';

-- ============================================================================
-- 5. SPECIAL TEST DATA VALIDATION
-- ============================================================================
PRINT '5. SPECIAL TEST DATA VALIDATION';
PRINT '--------------------------------';

-- Check for special character test records
DECLARE @SpecialCharCount INT;
SELECT @SpecialCharCount = COUNT(*)
FROM testepoint
WHERE SpecialChars IS NOT NULL
  AND SpecialChars != '';

IF @SpecialCharCount >= 3
    PRINT '✓ Special character test records present (' + CAST(@SpecialCharCount AS VARCHAR) + ')';
ELSE
    PRINT '✗ WARNING: Expected at least 3 special character test records';

-- Check for NULL value test records
DECLARE @NullTestCount INT;
SELECT @NullTestCount = COUNT(*)
FROM testepoint
WHERE Description IS NULL
   OR OwnerId IS NULL;

IF @NullTestCount >= 2
    PRINT '✓ NULL value test records present (' + CAST(@NullTestCount AS VARCHAR) + ')';
ELSE
    PRINT '✗ WARNING: Expected at least 2 NULL value test records';

-- Check for high-volume test records
DECLARE @HighVolumeCount INT;
SELECT @HighVolumeCount = COUNT(*)
FROM testepoint
WHERE RequestCount > 100000;

IF @HighVolumeCount >= 2
    PRINT '✓ High-volume test records present (' + CAST(@HighVolumeCount AS VARCHAR) + ')';
ELSE
    PRINT '✗ WARNING: Expected at least 2 high-volume test records';

-- Check for priority range test records
DECLARE @PriorityRangeCount INT;
SELECT @PriorityRangeCount = COUNT(DISTINCT Priority)
FROM testepoint
WHERE IsDeleted = 0;

IF @PriorityRangeCount >= 5
    PRINT '✓ Priority range test records present (' + CAST(@PriorityRangeCount AS VARCHAR) + ' distinct priorities)';
ELSE
    PRINT '✗ WARNING: Expected at least 5 distinct priority levels';

PRINT '';

-- ============================================================================
-- 6. FUNCTIONAL TEST QUERIES
-- ============================================================================
PRINT '6. FUNCTIONAL TEST QUERIES';
PRINT '--------------------------';

-- Test 6.1: Simple SELECT
PRINT 'Test 6.1: Simple SELECT';
DECLARE @SelectCount INT;
SELECT @SelectCount = COUNT(*)
FROM testepoint
WHERE IsActive = 1 AND IsDeleted = 0;
PRINT '  Active records: ' + CAST(@SelectCount AS VARCHAR);
IF @SelectCount > 0
    PRINT '✓ Simple SELECT works';
ELSE
    PRINT '✗ ERROR: Simple SELECT failed or no active records';

-- Test 6.2: LIKE operator
PRINT 'Test 6.2: LIKE operator';
DECLARE @LikeCount INT;
SELECT @LikeCount = COUNT(*)
FROM testepoint
WHERE EndpointName LIKE '%User%';
PRINT '  Records with "User" in name: ' + CAST(@LikeCount AS VARCHAR);
IF @LikeCount > 0
    PRINT '✓ LIKE operator works';
ELSE
    PRINT '✗ WARNING: LIKE operator test found no matches';

-- Test 6.3: JOIN-ready structure
PRINT 'Test 6.3: JOIN-ready structure';
DECLARE @OwnersWithEndpoints INT;
SELECT @OwnersWithEndpoints = COUNT(DISTINCT OwnerId)
FROM testepoint
WHERE OwnerId IS NOT NULL AND IsDeleted = 0;
PRINT '  Unique owners: ' + CAST(@OwnersWithEndpoints AS VARCHAR);
IF @OwnersWithEndpoints > 0
    PRINT '✓ JOIN-ready structure (has owner relationships)';

-- Test 6.4: Aggregate functions
PRINT 'Test 6.4: Aggregate functions';
DECLARE @AvgRequests DECIMAL(10,2), @MaxRequests INT, @MinRequests INT;
SELECT
    @AvgRequests = AVG(CAST(RequestCount AS DECIMAL(10,2))),
    @MaxRequests = MAX(RequestCount),
    @MinRequests = MIN(RequestCount)
FROM testepoint
WHERE IsDeleted = 0;
PRINT '  Avg requests: ' + CAST(@AvgRequests AS VARCHAR);
PRINT '  Max requests: ' + CAST(@MaxRequests AS VARCHAR);
PRINT '  Min requests: ' + CAST(@MinRequests AS VARCHAR);
IF @AvgRequests IS NOT NULL
    PRINT '✓ Aggregate functions work';
ELSE
    PRINT '✗ ERROR: Aggregate functions failed';

-- Test 6.5: ORDER BY
PRINT 'Test 6.5: ORDER BY';
SELECT TOP 1 @SelectCount = EndpointId
FROM testepoint
ORDER BY Priority DESC, CreatedDate DESC;
IF @SelectCount IS NOT NULL
    PRINT '✓ ORDER BY works';
ELSE
    PRINT '✗ ERROR: ORDER BY failed';

-- Test 6.6: Date filtering
PRINT 'Test 6.6: Date filtering';
DECLARE @DateFilterCount INT;
SELECT @DateFilterCount = COUNT(*)
FROM testepoint
WHERE CreatedDate >= '2025-01-01'
  AND CreatedDate < '2026-01-01';
PRINT '  Records created in 2025: ' + CAST(@DateFilterCount AS VARCHAR);
IF @DateFilterCount > 0
    PRINT '✓ Date filtering works';

PRINT '';

-- ============================================================================
-- 7. PERFORMANCE TEST QUERIES
-- ============================================================================
PRINT '7. PERFORMANCE TEST QUERIES';
PRINT '---------------------------';

-- Test 7.1: Index usage for Status filter
PRINT 'Test 7.1: Index usage check (Status)';
DECLARE @StatusFilterCount INT;
SELECT @StatusFilterCount = COUNT(*)
FROM testepoint WITH (INDEX(IX_testepoint_Status))
WHERE Status = 'ACTIVE' AND IsDeleted = 0;
PRINT '  Records with Status = ACTIVE: ' + CAST(@StatusFilterCount AS VARCHAR);
IF @StatusFilterCount > 0
    PRINT '✓ Status index usable';

-- Test 7.2: Index usage for Type filter
PRINT 'Test 7.2: Index usage check (Type)';
DECLARE @TypeFilterCount INT;
SELECT @TypeFilterCount = COUNT(*)
FROM testepoint WITH (INDEX(IX_testepoint_Type))
WHERE EndpointType = 'CRUD' AND IsDeleted = 0;
PRINT '  Records with Type = CRUD: ' + CAST(@TypeFilterCount AS VARCHAR);
IF @TypeFilterCount > 0
    PRINT '✓ Type index usable';

PRINT '';

-- ============================================================================
-- 8. SECURITY TEST DATA
-- ============================================================================
PRINT '8. SECURITY TEST DATA';
PRINT '---------------------';

-- Check for sensitive field data
DECLARE @ApiKeyCount INT, @SecretTokenCount INT;
SELECT @ApiKeyCount = COUNT(*) FROM testepoint WHERE ApiKey IS NOT NULL;
SELECT @SecretTokenCount = COUNT(*) FROM testepoint WHERE SecretToken IS NOT NULL;

PRINT '  Records with ApiKey: ' + CAST(@ApiKeyCount AS VARCHAR);
PRINT '  Records with SecretToken: ' + CAST(@SecretTokenCount AS VARCHAR);

IF @ApiKeyCount >= 5 AND @SecretTokenCount >= 5
    PRINT '✓ Sensitive field test data present';
ELSE
    PRINT '✗ WARNING: Limited sensitive field test data';

-- Check for SQL injection test strings
DECLARE @SqlInjectionTestCount INT;
SELECT @SqlInjectionTestCount = COUNT(*)
FROM testepoint
WHERE SpecialChars LIKE '%OR%'
   OR SpecialChars LIKE '%DROP%'
   OR SpecialChars LIKE '%SELECT%';

IF @SqlInjectionTestCount > 0
    PRINT '✓ SQL injection test strings present (' + CAST(@SqlInjectionTestCount AS VARCHAR) + ')';
ELSE
    PRINT '⚠ NOTE: No SQL injection test strings found';

PRINT '';

-- ============================================================================
-- 9. SUMMARY STATISTICS
-- ============================================================================
PRINT '9. SUMMARY STATISTICS';
PRINT '---------------------';

SELECT
    'Total Records' as Metric,
    COUNT(*) as Value
FROM testepoint

UNION ALL

SELECT
    'Active Records',
    COUNT(*)
FROM testepoint
WHERE IsActive = 1 AND IsDeleted = 0

UNION ALL

SELECT
    'Inactive Records',
    COUNT(*)
FROM testepoint
WHERE IsActive = 0 AND IsDeleted = 0

UNION ALL

SELECT
    'Deleted Records',
    COUNT(*)
FROM testepoint
WHERE IsDeleted = 1

UNION ALL

SELECT
    'Unique Endpoint Types',
    COUNT(DISTINCT EndpointType)
FROM testepoint

UNION ALL

SELECT
    'Unique Owners',
    COUNT(DISTINCT OwnerId)
FROM testepoint
WHERE OwnerId IS NOT NULL

UNION ALL

SELECT
    'Total Requests (Sum)',
    SUM(RequestCount)
FROM testepoint

UNION ALL

SELECT
    'Total Errors (Sum)',
    SUM(ErrorCount)
FROM testepoint

ORDER BY Metric;

PRINT '';

-- ============================================================================
-- 10. DETAILED BREAKDOWN BY STATUS AND TYPE
-- ============================================================================
PRINT '10. DETAILED BREAKDOWN';
PRINT '----------------------';

SELECT
    Status,
    EndpointType,
    COUNT(*) as Count,
    SUM(RequestCount) as TotalRequests,
    SUM(ErrorCount) as TotalErrors,
    AVG(AvgResponseTime) as AvgResponseTime
FROM testepoint
WHERE IsDeleted = 0
GROUP BY Status, EndpointType
ORDER BY Status, EndpointType;

PRINT '';

-- ============================================================================
-- VALIDATION COMPLETE
-- ============================================================================
PRINT '========================================';
PRINT 'VALIDATION COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'Review the results above:';
PRINT '  ✓ indicates successful validation';
PRINT '  ✗ indicates an error or warning';
PRINT '  ⚠ indicates an informational note';
PRINT '';
PRINT 'If all critical checks pass (✓), the test data is ready for use.';
PRINT 'Address any errors (✗) before proceeding with endpoint testing.';
PRINT '';
